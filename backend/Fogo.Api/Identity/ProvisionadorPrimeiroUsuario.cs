using System.Data;
using System.Data.Common;
using System.Text;
using Fogo.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fogo.Api;

public sealed class ProvisionadorPrimeiroUsuario(
    FogoDbContext banco,
    UserManager<UsuarioFogo> gerenciadorUsuarios)
{
    private const long ChaveBloqueioAdvisory = 0x464F474F;

    public async Task<bool> ExecutarAsync(CancellationToken cancelamento, bool somentePrimeiroUsuario = true)
    {
        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Este comando exige terminal interativo para receber a senha sem exibi-la.");
            return false;
        }

        var migracoesPendentes = await banco.Database.GetPendingMigrationsAsync(cancelamento);
        if (migracoesPendentes.Any())
        {
            Console.Error.WriteLine("Há migrations pendentes. Aplique-as antes de provisionar o primeiro usuário.");
            return false;
        }

        Console.Write("E-mail do primeiro usuário: ");
        var email = (Console.ReadLine() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            Console.Error.WriteLine("O e-mail é obrigatório.");
            return false;
        }

        var senha = LerSenha("Senha (mínimo de 12 caracteres e requisitos do Identity): ");
        var confirmacao = LerSenha("Confirme a senha: ");
        if (!string.Equals(senha, confirmacao, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("As senhas não coincidem.");
            return false;
        }

        var conexao = banco.Database.GetDbConnection();
        await banco.Database.OpenConnectionAsync(cancelamento);
        var bloqueioAdquirido = false;
        try
        {
            await ExecutarComandoAsync(conexao, "SELECT pg_advisory_lock(@chave)", cancelamento);
            bloqueioAdquirido = true;

            await using var transacao = await banco.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancelamento);
            var jaExisteUsuario = await gerenciadorUsuarios.Users.AnyAsync(cancelamento);
            if (somentePrimeiroUsuario && jaExisteUsuario)
            {
                Console.Error.WriteLine("Já existe pelo menos um usuário. O provisionamento inicial foi recusado.");
                return false;
            }

            var usuario = new UsuarioFogo
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            var resultado = await gerenciadorUsuarios.CreateAsync(usuario, senha);
            if (!resultado.Succeeded)
            {
                Console.Error.WriteLine("Não foi possível criar o usuário:");
                foreach (var erro in resultado.Errors)
                {
                    Console.Error.WriteLine($"- {erro.Description}");
                }

                return false;
            }

            await transacao.CommitAsync(cancelamento);
            Console.WriteLine(somentePrimeiroUsuario
                ? "Primeiro usuário criado. O endereço de e-mail foi confirmado pelo provisionamento local."
                : "Usuário criado. O endereço de e-mail foi confirmado pelo provisionamento local.");
            Console.WriteLine("Aviso: a aplicação ainda não define papéis ou permissões diferenciadas entre usuários autenticados.");
            return true;
        }
        finally
        {
            if (bloqueioAdquirido)
            {
                try
                {
                    await ExecutarComandoAsync(conexao, "SELECT pg_advisory_unlock(@chave)", CancellationToken.None);
                }
                finally
                {
                    await banco.Database.CloseConnectionAsync();
                }
            }
            else
            {
                await banco.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task ExecutarComandoAsync(
        DbConnection conexao,
        string texto,
        CancellationToken cancelamento)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = texto;
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "chave";
        parametro.DbType = DbType.Int64;
        parametro.Value = ChaveBloqueioAdvisory;
        comando.Parameters.Add(parametro);
        await comando.ExecuteScalarAsync(cancelamento);
    }

    private static string LerSenha(string instrucao)
    {
        Console.Write(instrucao);
        var senha = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return senha.ToString();
            }

            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (senha.Length > 0)
                {
                    senha.Length--;
                }

                continue;
            }

            if (!char.IsControl(tecla.KeyChar))
            {
                senha.Append(tecla.KeyChar);
            }
        }
    }
}
