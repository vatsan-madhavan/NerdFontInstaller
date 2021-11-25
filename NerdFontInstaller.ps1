<#
.SYNOPSIS
    Downloads and installs Nerd Font from latest Release or Pre-release of https://github.com/ryanoasis/nerd-fonts.
.DESCRIPTION
    Downloads and installs Nerd Fonts from https://github.com/ryanoasis/nerd-fonts.

    - By default, the script installs CascadiaCode (CascaydiaCove) Nerd Font from the latest Release avaialble at https://github.com/ryanoasis/nerd-fonts/releases. Prerelease fonts can be installed by specifying the '-Prerelease' switch.
    - If the fonts are already installed, then installation is skipped. This can be overrided by specifying the '-Force' switch.
    - Alternate fonts can be chosen supplying a different font at the commandline.

.EXAMPLE
    # Installs the Cascadia Code (Caskaydia Cove) Nerd Font
    PS > .\NerdFontInstaller.ps1
    Installing: CaskaydiaCove NF Book
    Installing: CaskaydiaCove Nerd Font Mono Book
    Installing: CaskaydiaCove NF Book
    Installing: CaskaydiaCove Nerd Font Book

.EXAMPLE
    # Installs the Agave Nerd Font
    PS > .\NerdFontInstaller.ps1 -Font Agave
    Installing: agave NF r
    Installing: agave Nerd Font Mono r
    Installing: agave NF r
    Installing: agave Nerd Font r

.PARAMETER Font
    Specifies the font to install.
    Default is 'CascadiaCode' Nerd Font (aka 'Cascaydia Cove').
    The full list of fonts supported are:
        - 3270
        - Agave
        - AnonymousPro
        - Arimo
        - AurulentSansMono
        - BigBlueTerminal
        - BitstreamVeraSansMono
        - CascadiaCode
        - CodeNewRoman
        - Cousine
        - DaddyTimeMono
        - DejaVuSansMono
        - DroidSansMono
        - FantasqueSansMono
        - FiraCode
        - FiraMono
        - Go-Mono
        - Gohu
        - Hack
        - Hasklig
        - HeavyData
        - Hermit
        - iA-Writer
        - IBMPlexMono
        - Inconsolata
        - InconsolataGo
        - InconsolataLGC
        - Iosevka
        - JetBrainsMono
        - Lekton
        - LiberationMono
        - Meslo
        - Monofur
        - Monoid
        - Mononoki
        - MPlus
        - Noto
        - OpenDyslexic
        - Overpass
        - ProFont
        - ProggyClean
        - RobotoMono
        - ShareTechMono
        - SourceCodePro
        - SpaceMono
        - Terminus
        - Tinos
        - Ubuntu
        - UbuntuMono
        - VictorMono
.PARAMETER Prerelease
    Downloads from the latest prerelease tag instead of the (default latest) release tag at https://github.com/ryanoasis/nerd-fonts/releases.
.PARAMETER Force
    Reinstalls fonts that are already installed. Default behavior is to skip installing fonts already present.
#>
[CmdletBinding()]
param (
    [ValidateSet('3270','Agave','AnonymousPro','Arimo','AurulentSansMono','BigBlueTerminal','BitstreamVeraSansMono','CascadiaCode','CodeNewRoman','Cousine','DaddyTimeMono','DejaVuSansMono','DroidSansMono','FantasqueSansMono','FiraCode','FiraMono','Go-Mono','Gohu','Hack','Hasklig','HeavyData','Hermit','iA-Writer','IBMPlexMono','Inconsolata','InconsolataGo','InconsolataLGC','Iosevka','JetBrainsMono','Lekton','LiberationMono','Meslo','Monofur','Monoid','Mononoki','MPlus','Noto','OpenDyslexic','Overpass','ProFont','ProggyClean','RobotoMono','ShareTechMono','SourceCodePro','SpaceMono','Terminus','Tinos','Ubuntu','UbuntuMono','VictorMono')]
    [string]$Font = 'CascadiaCode',
    [switch]$Prerelease,
    [switch]$Force
)

Set-StrictMode -Version Latest
#Requires -Assembly 'PresentationCore' # Used for [System.Windows.Media.GlyphTypeFace] to instead font files.
Add-Type -AssemblyName PresentationCore

<#
.SYNOPSIS
    Finds the asset download url for any Nerd Font
.PARAMETER fontName
    Name of the font
.PARAMETER preRelease
    True when a pre-release download link is requested, otherwise false.
#>
function Get-AssetDownloadUrl ([string]$fontName, [bool]$preRelease) {
    [string]$repo = "ryanoasis/nerd-fonts"

    Write-Verbose "Getting release information for https://github.com/$repo"
    $release = Invoke-RestMethod "https://api.github.com/repos/$repo/releases" | Where-Object {
        $_.prerelease -eq $preRelease
    } | Sort-Object -Property tag_name -Descending | Select-Object -First 1

    if (-not $release) {
        Write-Error "Failed to find release info" -ErrorAction Stop
    } else {
        Write-Verbose "Release Information:"
        foreach ($p in $release.PSObject.Properties) {
            Write-Verbose ("`t{0}: {1}" -f $p.Name, $p.Value)
        }
    }

    $assetInfo = $release.assets | Where-Object {
        $_.Name -ieq "$fontName.zip"
    } | Select-Object -First 1

    if (-not ($assetInfo)) {
        Write-Error "Download URL for $fontName could not be found" -ErrorAction Stop
    }

    $assetInfo.browser_download_url
}

<#
.SYNOPSIS
    Gets the font name
.PARAMETER fontFile
    Path to the font file
#>
function Get-FontName ([string]$fontFile) {
    $locale = (Get-WinSystemLocale).Name
    if (Test-Path -PathType Leaf -Path $fontFile) {
        $glyphTypeFace = [System.Windows.Media.GlyphTypeface]::new($fontFile)
        if ($glyphTypeFace) {
            '{0} {1}' -f $glyphTypeFace.Win32FamilyNames[$locale], $glyphTypeFace.Win32FaceNames[$locale]
        } else {
            [string]::Empty
        }
    } else {
        [string]::Empty
    }
}

# Create temp folder
$destinationPath = Join-path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid())
Write-Verbose "Staging location for zip file and fonts will be: $destinationPath"
New-Item -ItemType Directory -Path $destinationPath | Out-Null
if (-not (Test-Path -Path $destinationPath -PathType Container)) {
    Write-Error ("Destination Path '{0}' could not be created" -f $destinationPath) -ErrorAction Stop
}

try {
    $fontZipDownloadUrl = Get-AssetDownloadUrl $Font $Prerelease

    $fontsZip = Join-path $destinationPath "$Font.zip"
    Invoke-WebRequest $fontZipDownloadUrl -OutFile $fontsZip
    if (-not (Test-Path -Path $fontsZip)) {
        Write-Error ("Font Zip File '{0}' could not be created" -f $fontsZip) -ErrorAction Stop
    }
    Write-Verbose "Downloaded: $fontsZip"

    Expand-Archive $fontsZip -DestinationPath $destinationPath
    Write-Verbose "Unzipped: $fontsZip"

    Write-Verbose "Identifying *.ttf|*.otf files"
    $fontFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    @('*.ttf', '*.otf') | ForEach-Object {
        $filter = $_
        Get-ChildItem -Recurse -Filter $filter -Path $destinationPath | ForEach-Object {
            $fontFiles.Add($_)
        }
    }

    $fontFiles | ForEach-Object {
        Write-Verbose ("`tFound: {0}" -f $_.FullName)
    }

    $shellApp = New-Object -ComObject shell.application
    $fontsFolder = if ($shellApp) {
        $shellApp.NameSpace([int][System.Environment+SpecialFolder]::Fonts)
    }

    if (-not $fontsFolder) {
        Write-Error ("Can't install fonts: Failed to instantiate shell.application Font special folder ({0})" -f [System.Environment+SpecialFolder]::Fonts) -ErrorAction Stop
    }

    Write-Verbose "Successfully created shell.Application ComObject"

    $installedFonts = $fontsFolder.Items() | Select-Object -Property Name
    $locale = (Get-WinSystemLocale).Name

    [bool]$installed = $false
    foreach ($fontFile in $fontFiles) {
        $fontName = Get-FontName $fontFile.FullName

        [bool]$preInstalled = $false
        if ($installedFonts.Name -icontains $fontName) {
            $preInstalled = $true
        }

        if ($preInstalled) {
            if (-not $Force) {
                Write-Output ("`tSkipping: {0} already installed" -f $fontName)
            } else {
                Write-Output ("`tReinstalling: {0}. Select 'Yes' if you receive a prompt." -f $fontName)
                $fontsFolder.MoveHere($fontFile.FullName)
                $installed = $true
            }
        } else {
            Write-Output ("Installing: {0}" -f $fontName)
            $fontsFolder.MoveHere($fontFile.FullName)
            $installed = $true
        }
    }
} finally {
    # Remove temp folder
    Remove-Item -Force -Recurse $destinationPath | Out-Null
    Write-Verbose "Removed $destinationPath"
}
