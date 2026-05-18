class Neuron {
    [Value[]]$w
    [Value]$b

    Neuron([int]$nin) {
        $this.w = for ($i = 0; $i -lt $nin; $i++) {
            [Value]::new(([Random]::Shared.NextDouble() * 2 - 1), "w$i")
        }

        $this.b = [Value]::new(([Random]::Shared.NextDouble() * 2 - 1), "b")
    }

    [Value] Invoke([Value[]]$x) {
        if ($x.Count -ne $this.w.Count) {
            throw "Expected $($this.w.Count) inputs, got $($x.Count)."
        }

        $sum = $this.b
        for ($i = 0; $i -lt $this.w.Count; $i++) {
            $sum = $sum + ($this.w[$i] * $x[$i])
        }

        return $sum.Tanh()
    }

    [Value[]] parameters(){
        return @($this.w) + @($this.b)
    }
}

class Layer {
    [Neuron[]]$neurons = @()

    Layer([int]$nin, [int]$nout) {
        $this.neurons = for ($i = 0; $i -lt $nout; $i++) {
            [Neuron]::new($nin)
        }
    }

    [Value[]]Invoke([Value[]]$x) {
        $out = @()
        $out = foreach ($neuron in $this.neurons) {
            $neuron.Invoke($x)
        }

        return $out
    }

    [Value[]] parameters(){
        [Value[]]$params = @()
        foreach ($n in $this.neurons){
            $params += $n.parameters()
        }

        return $params
    }
}

class MLP {
    [Layer[]]$layers = @()

    MLP([int]$nin, [int[]]$nouts) {
        $sizes = @($nin) + $nouts

        $this.layers = for ($i = 0; $i -lt $nouts.Count; $i++) {
            [Layer]::new($sizes[$i], $sizes[$i + 1])
        }
    }

    [Value[]]Invoke([Value[]]$x) {
        $out = $x
        
        foreach ($layer in $this.layers) {
            $out = $layer.Invoke($out)
        }

        return $out;
    }
    [Value[]] parameters(){
        [Value[]]$params = @()
        foreach ($l in $this.layers){
            $params += $l.parameters()
        }

        return $params
    }
}
