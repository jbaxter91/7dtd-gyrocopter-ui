$base = 'D:\SteamLibrary\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed'
$loading = New-Object 'System.Collections.Generic.HashSet[string]'
$handler = [System.ResolveEventHandler]{ param($sender,$e)
    $name = $e.Name.Split(',')[0] + '.dll'
    if ($loading.Contains($name)) { return $null }
    $path = Join-Path $base $name
    if (Test-Path $path) {
        $loading.Add($name) | Out-Null
        return [System.Reflection.Assembly]::LoadFrom($path)
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($handler) | Out-Null
$asm = [System.Reflection.Assembly]::LoadFrom((Join-Path $base 'Assembly-CSharp.dll'))
$type = $asm.GetType('EntityAlive')
"OnEntityDeath methods:";
$type.GetMethods([System.Reflection.BindingFlags] 'Instance, NonPublic, Public') |
    Where-Object { $_.Name -eq 'OnEntityDeath' } |
    ForEach-Object { $_.ToString() };
"Kill overloads:";
$type.GetMethods([System.Reflection.BindingFlags] 'Instance, NonPublic, Public') |
    Where-Object { $_.Name -eq 'Kill' } |
    ForEach-Object { $_.ToString() };

"DamageResponse fields:";
$dr = $asm.GetType('DamageResponse');
$dr.GetFields([System.Reflection.BindingFlags] 'Instance, Public, NonPublic') |
    ForEach-Object { $_.Name + ' :: ' + $_.FieldType.FullName };

"DamageSource fields:";
$ds = $asm.GetType('DamageSource');
$ds.GetFields([System.Reflection.BindingFlags] 'Instance, Public, NonPublic') |
    ForEach-Object { $_.Name + ' :: ' + $_.FieldType.FullName };
