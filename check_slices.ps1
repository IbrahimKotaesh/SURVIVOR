$viniMeta = Get-Content "E:/SURVIVOR/My project/Assets/Resources/vini.png.meta"
$vergilMeta = Get-Content "E:/SURVIVOR/My project/Assets/Resources/vergil_van_dijk_2.png.meta"

function PrintFirstSliceInfo($meta, $name) {
    Write-Host "=== $name ==="
    $inSprites = $false
    $count = 0
    for ($i = 0; $i -lt $meta.Count; $i++) {
        if ($meta[$i] -match "sprites:") {
            $inSprites = $true
        }
        if ($inSprites -and $meta[$i] -match "rect:") {
            for ($j = 0; $j -lt 15; $j++) {
                Write-Host $meta[$i + $j]
            }
            break
        }
    }
}

PrintFirstSliceInfo $viniMeta "Vini"
PrintFirstSliceInfo $vergilMeta "Vergil"
