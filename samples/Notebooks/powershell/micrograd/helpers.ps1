function Zip {
    param(
        [Parameter(Mandatory)]
        [object[]]$Left,

        [Parameter(Mandatory)]
        [object[]]$Right
    )

    $count = [Math]::Min($Left.Count, $Right.Count)

    for ($i = 0; $i -lt $count; $i++) {
        [pscustomobject]@{
            Left  = $Left[$i]
            Right = $Right[$i]
        }
    }
}

function Sum-Value {
    param([scriptblock]$Selector)

    begin { $sum = [Value]::new(0.0, 'loss') }
    process { $sum = $sum + (& $Selector $_) }
    end { $sum }
}