$ErrorActionPreference = 'Stop'

$saida = Join-Path $PSScriptRoot 'resultado.csv'
$agente = 'FOGO-public-camera-geocoder/1.0 (one-time geocoding for the F.O.G.O. public camera registry)'
$campos = @('cidade', 'ponto_indicado_no_portal', 'consulta', 'latitude', 'longitude', 'status', 'motivo', 'display_name', 'osm_type', 'osm_id', 'categoria', 'tipo', 'resposta_bruta')
$utf8 = New-Object System.Text.UTF8Encoding($true)
[void][System.Reflection.Assembly]::LoadWithPartialName('System.Net.Http')
$cliente = New-Object System.Net.Http.HttpClient
$cliente.DefaultRequestHeaders.UserAgent.ParseAdd($agente)
$cliente.DefaultRequestHeaders.Accept.ParseAdd('application/json')

$pontos = @(
    @('Avaré', 'Mercadão Frente'),
    @('Avaré', 'Mercadão Fundos'),
    @('Avaré', 'Portal Entrada Avaré'),
    @('Avaré', 'R.G do Sul x D.Santana'),
    @('Avaré', 'R.G do Sul x Maranhão'),
    @('Bastos', 'Areninha'),
    @('Bastos', 'Entrada Kawakami'),
    @('Bastos', 'Entrada Prefeitura'),
    @('Bastos', 'Praça 500 anos'),
    @('Bastos', 'Praça da Juventude'),
    @('Bastos', 'Praça do Ovo'),
    @('Bastos', 'Praça S.J.Operário'),
    @('Bastos', 'Praça Senjiro Hatanaka'),
    @('Bastos', 'Entrada Yabuta'),
    @('Cambará', 'Genaro Resende 1'),
    @('Cambará', 'Genaro Resende 2'),
    @('Cambará', 'Parque Ignez Panichi'),
    @('Cerqueira César', 'Areninha'),
    @('Cerqueira César', 'Areninha 2'),
    @('Cerqueira César', 'Patio Estação'),
    @('Cerqueira César', 'Patio Estação 2'),
    @('Cerqueira César', 'Praça Matriz'),
    @('Cerqueira César', 'Praça Matriz 2'),
    @('Cerqueira César', 'Rodoviária'),
    @('Cândido Mota', 'Parque Panarao'),
    @('Cândido Mota', 'Praça Igreja Popular'),
    @('Cândido Mota', 'Praça Matriz 1'),
    @('Cândido Mota', 'Praça Matriz 2'),
    @('Cândido Mota', 'Rotátoria do Cristo'),
    @('Espírito Santo do Turvo', 'Praça Matriz'),
    @('Espírito Santo do Turvo', 'Praça José Melo'),
    @('Jaú', 'Av. Anna Claudina'),
    @('Jaú', 'Cruzamento Av. João Ferraz X Isaltino do Amaral Carvalho'),
    @('Jaú', 'Cruzamento Av. Zezinho Magalhães X Av. Anna Claudina'),
    @('Jaú', 'Lago do Silvério 1'),
    @('Jaú', 'Lago do Silvério 2'),
    @('Jaú', 'Museu Municipal'),
    @('Jaú', 'Parque do Rio Jaú 1'),
    @('Jaú', 'Parque do Rio Jaú 2'),
    @('Jaú', 'Kartódromo 1'),
    @('Jaú', 'Kartódromo 2'),
    @('Jaú', 'Praça da República 1'),
    @('Jaú', 'Praça da República 2'),
    @('Ourinhos', 'Calçadão'),
    @('Ourinhos', 'Calçadão 2'),
    @('Ourinhos', 'Praça Recanto dos Pássaros'),
    @('Ourinhos', 'Plaza Shopping'),
    @('Ourinhos', 'Feira da lua Cohab'),
    @('Ourinhos', 'Praça dos Skatistas'),
    @('Ourinhos', 'Praça Kennedy'),
    @('Ourinhos', 'Praça dos Ferroviários'),
    @('Ourinhos', 'Praça São Pio X'),
    @('Ourinhos', 'Praça Vila Perino'),
    @('Ourinhos', 'Rio de Janeiro x Antônio Carlos Mori'),
    @('Ourinhos', 'Altino Arantes x Cardoso Ribeiro'),
    @('Paraguaçu Paulista', 'Avenida Brasil'),
    @('Paraguaçu Paulista', 'Balneário Municipal'),
    @('Paraguaçu Paulista', 'Centro de Convergência'),
    @('Paraguaçu Paulista', 'Fonte Luminosa'),
    @('Paraguaçu Paulista', 'Maria Fumaça'),
    @('Paraguaçu Paulista', 'Portal da Entrada'),
    @('Paraguaçu Paulista', 'Praça Matriz'),
    @('Paraguaçu Paulista', 'Praça Panambi'),
    @('Paraguaçu Paulista', 'Prefeitura Municipal'),
    @('Quatá', 'Praça Matriz'),
    @('Quatá', 'Praça da Fonte'),
    @('Quatá', 'Praça Valéria'),
    @('Quatá', 'Praça do Campo Sintético'),
    @('Quatá', 'Praça Jardim Tropical'),
    @('Ribeirão do Sul', 'Entrada cristo'),
    @('Ribeirão do Sul', 'Matriz'),
    @('São José da Boa Vista', 'Entrada do Santo'),
    @('São José da Boa Vista', 'Praça da Igreja'),
    @('São José da Boa Vista', 'Praça do Estádio'),
    @('São José da Boa Vista', 'Praça José Maria Soares'),
    @('Taguaí', 'Entrada do Cristo'),
    @('Taguaí', 'Entada e saida de Alunos -Pe. Giovanne'),
    @('Taguaí', 'Lanchodromo'),
    @('Taguaí', 'Praça Angelo Globo'),
    @('Taguaí', 'Praça Lanchodromo')
)

function Normalizar([string] $valor) {
    if ([string]::IsNullOrWhiteSpace($valor)) { return '' }
    $semAcentos = $valor.Normalize([Text.NormalizationForm]::FormD) -replace '\p{Mn}', ''
    return ([regex]::Replace($semAcentos.ToLowerInvariant(), '[^a-z0-9]+', ' ')).Trim()
}

function Nova-Linha([string] $cidade, [string] $ponto, [string] $consulta) {
    return [ordered]@{
        cidade = $cidade
        ponto_indicado_no_portal = $ponto
        consulta = $consulta
        latitude = ''
        longitude = ''
        status = 'PENDENTE_VALIDACAO'
        motivo = ''
        display_name = ''
        osm_type = ''
        osm_id = ''
        categoria = ''
        tipo = ''
        resposta_bruta = ''
    }
}

function Consulta-Para([string] $cidade, [string] $ponto) {
    $nome = $ponto
    if ($ponto -match '\s+x\s+') {
        $semPrefixo = $ponto -replace '(?i)^Cruzamento\s+', ''
        $partes = [regex]::Split($semPrefixo, '(?i)\s+x\s+', 2)
        $nome = ($partes[0].Trim() + ' & ' + $partes[1].Trim())
    }
    return "$nome, $cidade, São Paulo, Brasil"
}

function Cidade-EstadoCorretos($candidato, [string] $cidade) {
    $endereco = $candidato.address
    if (-not $endereco) { return $false }
    $localidades = @($endereco.city, $endereco.town, $endereco.village, $endereco.municipality, $endereco.county, $endereco.city_district)
    $cidadeEsperada = Normalizar $cidade
    $localidadeOk = $false
    foreach ($localidade in $localidades) {
        if (-not [string]::IsNullOrWhiteSpace([string]$localidade) -and (Normalizar ([string]$localidade)) -eq $cidadeEsperada) {
            $localidadeOk = $true
        }
    }
    return $localidadeOk -and ((Normalizar ([string]$endereco.state)) -eq 'sao paulo')
}

function Cidade-Generica($candidato) {
    $categoria = [string]$candidato.category
    if (-not $categoria) { $categoria = [string]$candidato.class }
    $tipo = Normalizar ([string]$candidato.type)
    return ((Normalizar $categoria) -in @('boundary', 'place')) -and ($tipo -in @('administrative', 'city', 'town', 'village', 'municipality', 'county', 'state'))
}

function Consultar-Nominatim([string] $consulta) {
    $parametros = [System.Collections.Generic.Dictionary[string,string]]::new()
    $parametros['q'] = $consulta
    $parametros['format'] = 'jsonv2'
    $parametros['addressdetails'] = '1'
    $parametros['limit'] = '5'
    $parametros['countrycodes'] = 'br'
    $query = ($parametros.GetEnumerator() | ForEach-Object {
        [uri]::EscapeDataString($_.Key) + '=' + [uri]::EscapeDataString($_.Value)
    }) -join '&'
    $resposta = $cliente.GetAsync("https://nominatim.openstreetmap.org/search?$query").GetAwaiter().GetResult()
    $bruto = $resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    if (-not $resposta.IsSuccessStatusCode) {
        return @{ resultados = @(); bruto = $bruto; erro = "HTTP $([int]$resposta.StatusCode)" }
    }
    try {
        $resultados = ConvertFrom-Json -InputObject $bruto
        return @{ resultados = $resultados; bruto = $bruto; erro = '' }
    } catch {
        return @{ resultados = @(); bruto = $bruto; erro = "JSON inválido: $($_.Exception.Message)" }
    }
}

function Avaliar([string] $cidade, [string] $ponto, [string] $consulta, $resultados, [string] $bruto, [string] $erro) {
    $linha = Nova-Linha $cidade $ponto $consulta
    $linha.resposta_bruta = $bruto
    if ($erro) {
        $linha.motivo = "Falha na consulta: $erro"
        return $linha
    }
    if (@($resultados).Count -eq 0) {
        $linha.motivo = 'Nominatim não retornou candidatos.'
        return $linha
    }

    $candidatosLocais = @($resultados | Where-Object { (Cidade-EstadoCorretos $_ $cidade) -and -not (Cidade-Generica $_) })
    if ($candidatosLocais.Count -eq 0) {
        $estados = @($resultados | ForEach-Object { [string]$_.address.state } | Where-Object { $_ } | Select-Object -Unique)
        if ($estados.Count -gt 0) {
            $linha.motivo = "Sem resultado específico em $cidade, São Paulo; resposta da API aponta: $($estados -join ', ')."
        } else {
            $linha.motivo = 'Sem resultado específico confirmado na cidade e no estado de São Paulo.'
        }
        return $linha
    }

    $primeiro = $candidatosLocais[0]
    if ($candidatosLocais.Count -gt 1) {
        $ids = @($candidatosLocais | ForEach-Object { "$($_.osm_type):$($_.osm_id)" } | Select-Object -Unique)
        if ($ids.Count -gt 1) {
            $linha.motivo = 'Nominatim retornou múltiplos locais distintos na cidade; candidato ambíguo.'
            return $linha
        }
    }

    $nomeBase = $ponto -replace '(?i)\s+(frente|fundos|1|2)$', ''
    $nomeBusca = Normalizar $nomeBase
    $nomeResultado = Normalizar ([string]$primeiro.name)
    $display = Normalizar ([string]$primeiro.display_name)
    $ruas = @()
    if ($ponto -match '\s+x\s+') {
        $semPrefixo = $ponto -replace '(?i)^Cruzamento\s+', ''
        $ruas = @([regex]::Split($semPrefixo, '(?i)\s+x\s+', 2) | ForEach-Object { Normalizar $_ })
    }

    if ($ruas.Count -gt 0 -and @($ruas | Where-Object { $_ -and $display.Contains($_) }).Count -eq $ruas.Count) {
        $linha.status = 'VALIDADO'
        $linha.motivo = 'Cruzamento nominalmente compatível, com cidade e estado confirmados pelo Nominatim.'
    } else {
        $ignorar = @('praca', 'avenida', 'av', 'rua', 'cruzamento', 'municipal', 'do', 'da', 'de', 'dos', 'das', 'x')
        $tokens = @($nomeBusca -split '\s+' | Where-Object { $_.Length -ge 3 -and $_ -notin $ignorar })
        $cobertura = 0
        if ($tokens.Count -gt 0) {
            $cobertura = @($tokens | Where-Object { $nomeResultado.Contains($_) }).Count / $tokens.Count
        }
        if (-not $nomeResultado -or (-not $nomeBusca.Contains($nomeResultado) -and $cobertura -lt 0.6)) {
            $linha.motivo = 'Resultado na cidade, mas o nome não identifica com segurança o ponto indicado.'
            return $linha
        }
        $linha.status = 'REFERENCIA_PUBLICA'
        $linha.motivo = 'Referência pública reconhecida pelo Nominatim; posição exata da câmera não confirmada.'
    }

    $linha.latitude = [string]$primeiro.lat
    $linha.longitude = [string]$primeiro.lon
    $linha.display_name = [string]$primeiro.display_name
    $linha.osm_type = [string]$primeiro.osm_type
    $linha.osm_id = [string]$primeiro.osm_id
    $linha.categoria = [string]$primeiro.category
    if (-not $linha.categoria) { $linha.categoria = [string]$primeiro.class }
    $linha.tipo = [string]$primeiro.type
    return $linha
}

function Gravar-Csv($linhas) {
    $registros = @($linhas | ForEach-Object {
        [pscustomobject]@{
            cidade = $_.cidade
            ponto_indicado_no_portal = $_.ponto_indicado_no_portal
            latitude = $_.latitude
            longitude = $_.longitude
            status = $_.status
            local_encontrado = $_.display_name
            place_id_ou_osm_id = if ($_.osm_id) { "$($_.osm_type):$($_.osm_id)" } else { '' }
            observacoes = "$($_.motivo) Fonte: Nominatim/OpenStreetMap; © OpenStreetMap contributors, ODbL 1.0 (https://www.openstreetmap.org/copyright)."
        }
    })
    $conteudo = @($registros | ConvertTo-Csv -Delimiter ';' -NoTypeInformation)
    [System.IO.File]::WriteAllLines($saida, $conteudo, $utf8)
}

if ($pontos.Count -ne 80) { throw "A lista tem $($pontos.Count) pontos, mas deveria ter 80." }

$cache = @{}
if (Test-Path -LiteralPath $saida) {
    foreach ($registro in (Import-Csv -LiteralPath $saida -Delimiter ';')) {
        $cache[([string]$registro.cidade + '|' + [string]$registro.ponto_indicado_no_portal)] = $registro
    }
}

$linhas = [System.Collections.Generic.List[object]]::new()
$cronometro = [System.Diagnostics.Stopwatch]::new()
$requisicaoAnterior = $null
for ($i = 0; $i -lt $pontos.Count; $i++) {
    $cidade = [string]$pontos[$i][0]
    $ponto = [string]$pontos[$i][1]
    $consulta = Consulta-Para $cidade $ponto
    $chave = "$cidade|$ponto"

    if ($cache.ContainsKey($chave) -and $cache[$chave].resposta_bruta) {
        $brutoCache = [string]$cache[$chave].resposta_bruta
        if ($i -lt 3) { Write-Host "`n--- RESPOSTA BRUTA $($i + 1)/3 | $consulta ---`n$brutoCache`n--- FIM RESPOSTA ---" }
        $resultadosCache = ConvertFrom-Json -InputObject $brutoCache
        $linha = Avaliar $cidade $ponto $consulta $resultadosCache $brutoCache ''
        $linhas.Add($linha)
        Write-Host ('[{0:D2}/80] cache: {1} — {2}' -f ($i + 1), $cidade, $ponto)
        continue
    }

    if ($requisicaoAnterior) {
        $decorrido = ([datetime]::UtcNow - $requisicaoAnterior).TotalMilliseconds
        $restante = 1100 - $decorrido
        if ($restante -gt 0) { [System.Threading.Thread]::Sleep([int][math]::Ceiling($restante)) }
    }

    try {
        $resultadoApi = Consultar-Nominatim $consulta
        $bruto = [string]$resultadoApi.bruto
        $erro = [string]$resultadoApi.erro
        $resultados = $resultadoApi.resultados
    } catch {
        $bruto = ''
        $erro = $_.Exception.Message
        $resultados = @()
    }
    $requisicaoAnterior = [datetime]::UtcNow
    if ($i -lt 3) {
        $exibicaoBruta = $bruto
        if (-not $exibicaoBruta) { $exibicaoBruta = $erro }
        Write-Host "`n--- RESPOSTA BRUTA $($i + 1)/3 | $consulta ---`n$exibicaoBruta`n--- FIM RESPOSTA ---"
    }

    $linha = Avaliar $cidade $ponto $consulta $resultados $bruto $erro
    $linhas.Add($linha)
    $provisorias = [System.Collections.Generic.List[object]]::new()
    foreach ($item in $linhas) { $provisorias.Add($item) }
    for ($j = $i + 1; $j -lt $pontos.Count; $j++) {
        $pendente = Nova-Linha ([string]$pontos[$j][0]) ([string]$pontos[$j][1]) (Consulta-Para ([string]$pontos[$j][0]) ([string]$pontos[$j][1]))
        $pendente.motivo = 'Aguardando consulta.'
        $provisorias.Add($pendente)
    }
    Gravar-Csv $provisorias
    Write-Host "[$($i + 1)/80] consultado: $cidade - $ponto -> $($linha.status)"
}

$grupos = @{}
for ($i = 0; $i -lt $pontos.Count; $i++) {
    $cidade = [string]$pontos[$i][0]
    $ponto = [string]$pontos[$i][1]
    $base = ($ponto -replace '(?i)\s+(frente|fundos|1|2)$', '').Trim()
    if ($base -ne $ponto) {
        $chave = "$cidade|$(Normalizar $base)"
        if (-not $grupos.ContainsKey($chave)) { $grupos[$chave] = [System.Collections.Generic.List[int]]::new() }
        $grupos[$chave].Add($i)
    }
}

foreach ($grupo in $grupos.Values) {
    if ($grupo.Count -lt 2) { continue }
    $ids = @($grupo | ForEach-Object { "$($linhas[$_].osm_type):$($linhas[$_].osm_id)" } | Where-Object { $_ -ne ':' } | Select-Object -Unique)
    $pendenteNoGrupo = @($grupo | Where-Object { $linhas[$_].status -eq 'PENDENTE_VALIDACAO' }).Count -gt 0
    if ($ids.Count -ne 1 -or $pendenteNoGrupo) {
        foreach ($indice in $grupo) {
            $linhas[$indice].latitude = ''
            $linhas[$indice].longitude = ''
            $linhas[$indice].status = 'PENDENTE_VALIDACAO'
            $linhas[$indice].motivo = 'Variante Frente/Fundos/1/2 sem identidade OSM comum confirmada; não foi atribuída posição distinta.'
        }
    } else {
        $referencia = $linhas[$grupo[0]]
        foreach ($indice in $grupo) {
            $linhas[$indice].latitude = $referencia.latitude
            $linhas[$indice].longitude = $referencia.longitude
            $linhas[$indice].status = 'REFERENCIA_PUBLICA'
            $linhas[$indice].motivo = 'Mesma referência OSM para variantes Frente/Fundos/1/2; não representa a posição exata da câmera.'
        }
    }
}

Gravar-Csv $linhas
Write-Host "`nCSV: $saida"
Write-Host "Total: $($linhas.Count)"
foreach ($status in @('VALIDADO', 'REFERENCIA_PUBLICA', 'PENDENTE_VALIDACAO')) {
    $total = @($linhas | Where-Object { $_.status -eq $status }).Count
    Write-Host "${status}: $total"
}
$cliente.Dispose()
