<#
.SYNOPSIS
    Crea la base (si no existe) y aplica los scripts pendientes de db/sqlserver en orden.

.DESCRIPTION
    1. Ejecuta crear_base.sql contra master (crea GestionProyectos y valida collation *_CI_*).
    2. Aplica scripts/V###__*.sql en orden; omite las versiones ya registradas en dbo.VersionesEsquema.
    3. Aplica seed/S###__*.sql en orden; siempre se ejecutan (son idempotentes) salvo -OmitirSeed.
    Cualquier error corta la ejecucion (sqlcmd -b).

.EXAMPLE
    .\db\sqlserver\aplicar.ps1
    .\db\sqlserver\aplicar.ps1 -Servidor "(localdb)\MSSQLLocalDB" -Base GestionProyectos
#>
[CmdletBinding()]
param(
    [string]$Servidor = '(localdb)\MSSQLLocalDB',
    [string]$Base = 'GestionProyectos',
    [switch]$OmitirSeed
)

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot

function Invoke-SqlArchivo([string]$baseDestino, [string]$archivo) {
    Write-Host "-> $(Split-Path $archivo -Leaf)"
    # -b: corta ante error; -I: QUOTED_IDENTIFIER ON; -f 65001: archivos UTF-8
    & sqlcmd -S $Servidor -d $baseDestino -E -b -I -f 65001 -i $archivo
    if ($LASTEXITCODE -ne 0) { throw "Error aplicando $archivo (codigo $LASTEXITCODE)." }
}

function Get-VersionesAplicadas {
    $q = "SET NOCOUNT ON; IF OBJECT_ID(N'dbo.VersionesEsquema', N'U') IS NOT NULL SELECT Version FROM dbo.VersionesEsquema;"
    $salida = & sqlcmd -S $Servidor -d $Base -E -b -h -1 -W -Q $q
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo leer dbo.VersionesEsquema.' }
    return @($salida | Where-Object { $_ -and $_.Trim() } | ForEach-Object { $_.Trim() })
}

# 1. Base
Invoke-SqlArchivo 'master' (Join-Path $raiz 'crear_base.sql')

# 2. Scripts de esquema pendientes
$aplicadas = Get-VersionesAplicadas
Get-ChildItem (Join-Path $raiz 'scripts') -Filter 'V*__*.sql' | Sort-Object Name | ForEach-Object {
    $version = ($_.Name -split '__')[0]
    if ($aplicadas -contains $version) {
        Write-Host "   $version ya aplicada, se omite."
    } else {
        Invoke-SqlArchivo $Base $_.FullName
    }
}

# 3. Semilla (idempotente)
if (-not $OmitirSeed) {
    Get-ChildItem (Join-Path $raiz 'seed') -Filter 'S*__*.sql' | Sort-Object Name | ForEach-Object {
        Invoke-SqlArchivo $Base $_.FullName
    }
}

# 4. Verificacion
& sqlcmd -S $Servidor -d $Base -E -b -W -Q "SET NOCOUNT ON; SELECT Version, Descripcion, FechaAplicacion FROM dbo.VersionesEsquema ORDER BY Version;"
