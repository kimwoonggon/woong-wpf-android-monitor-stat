param(
    [string[]]$HierarchyPath = @(),
    [int]$MaxBlankFloorPx = 80,
    [int]$MaxSystemNavigationOverlapPx = 1,
    [switch]$EnforceSystemNavigationSeparation
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ($HierarchyPath.Count -eq 0) {
    $defaultCandidates = @(
        "artifacts/android-app-switch-qa/latest/dashboard-after-app-switch.xml",
        "artifacts/android-app-switch-qa/latest/sessions-after-app-switch.xml",
        "artifacts/android-ui-regression/latest/tree-dashboard.xml",
        "artifacts/android-ui-regression/latest/android-bottom-nav-after-fix.xml"
    )
    $HierarchyPath = @($defaultCandidates | ForEach-Object { Join-Path $repoRoot $_ } | Where-Object {
            Test-Path -LiteralPath $_
        })
}

if ($HierarchyPath.Count -eq 0) {
    throw "No Android UI hierarchy XML paths were provided or found."
}

function Get-XmlDocument {
    param([string]$Path)

    $raw = Get-Content -Raw -LiteralPath $Path
    $endIndex = $raw.IndexOf("</hierarchy>", [System.StringComparison]::OrdinalIgnoreCase)
    if ($endIndex -lt 0) {
        throw "Android UI hierarchy does not contain a closing hierarchy element: $Path"
    }

    $xmlText = $raw.Substring(0, $endIndex + "</hierarchy>".Length)
    return [xml]$xmlText
}

function Get-Bounds {
    param([System.Xml.XmlElement]$Node)

    $bounds = $Node.GetAttribute("bounds")
    if ($bounds -notmatch "^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$") {
        return $null
    }

    return [pscustomobject]@{
        left = [int]$Matches[1]
        top = [int]$Matches[2]
        right = [int]$Matches[3]
        bottom = [int]$Matches[4]
    }
}

function Get-DescendantNodes {
    param([System.Xml.XmlElement]$Node)

    $descendants = New-Object System.Collections.Generic.List[System.Xml.XmlElement]
    foreach ($child in $Node.ChildNodes) {
        if ($child -is [System.Xml.XmlElement]) {
            $descendants.Add($child)
            foreach ($descendant in Get-DescendantNodes -Node $child) {
                $descendants.Add($descendant)
            }
        }
    }

    return $descendants
}

function Test-BottomNavHierarchy {
    param([string]$Path)

    $document = Get-XmlDocument -Path $Path
    $bottomNavigationNodes = @($document.SelectNodes("//*[@resource-id='com.woong.monitorstack:id/bottomNavigation']"))
    if ($bottomNavigationNodes.Count -eq 0) {
        Write-Host "Android bottom navigation floor: SKIP (no bottomNavigation node) - $Path"
        return
    }

    $navigationBarNodes = @($document.SelectNodes("//*[@resource-id='android:id/navigationBarBackground']"))
    $navigationBarTop = $null
    if ($navigationBarNodes.Count -gt 0) {
        $navigationBarTop = @($navigationBarNodes | ForEach-Object { Get-Bounds -Node $_ } | Where-Object {
                $null -ne $_
            } | Sort-Object top | Select-Object -First 1).top
    }

    foreach ($bottomNavigation in $bottomNavigationNodes) {
        $bottomNavigationBounds = Get-Bounds -Node $bottomNavigation
        if ($null -eq $bottomNavigationBounds) {
            throw "bottomNavigation node is missing parseable bounds in $Path"
        }

        $descendants = Get-DescendantNodes -Node $bottomNavigation
        $visibleItemNodes = @($descendants | Where-Object {
                $resourceId = $_.GetAttribute("resource-id")
                $resourceId -match "com\.woong\.monitorstack:id/(navigation_bar_item_content_container|navigation_bar_item_labels_group|navigation_bar_item_(large|small)_label_view)"
            })

        if ($visibleItemNodes.Count -eq 0) {
            throw "bottomNavigation has no parseable tab item descendants in $Path"
        }

        $maxVisibleItemBottom = @($visibleItemNodes | ForEach-Object { Get-Bounds -Node $_ } | Where-Object {
                $null -ne $_
            } | Measure-Object -Property bottom -Maximum).Maximum

        $blankFloorPx = $bottomNavigationBounds.bottom - [int]$maxVisibleItemBottom
        if ($blankFloorPx -gt $MaxBlankFloorPx) {
            throw "Android bottom navigation blank floor is ${blankFloorPx}px in $Path; max allowed is ${MaxBlankFloorPx}px. bottomNavigation=$($bottomNavigation.GetAttribute("bounds")), deepest tab item bottom=$maxVisibleItemBottom."
        }

        if ($EnforceSystemNavigationSeparation -and $null -ne $navigationBarTop) {
            $systemNavigationOverlapPx = $bottomNavigationBounds.bottom - [int]$navigationBarTop
            if ($systemNavigationOverlapPx -gt $MaxSystemNavigationOverlapPx) {
                throw "Android bottom navigation overlaps the system navigation bar by ${systemNavigationOverlapPx}px in $Path; max allowed is ${MaxSystemNavigationOverlapPx}px. bottomNavigation=$($bottomNavigation.GetAttribute("bounds")), navigationBarBackgroundTop=$navigationBarTop."
            }
        }

        Write-Host "Android bottom navigation floor: PASS ($blankFloorPx px blank floor) - $Path"
    }
}

foreach ($path in $HierarchyPath) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Android UI hierarchy XML is missing: $path"
    }

    Test-BottomNavHierarchy -Path $path
}
