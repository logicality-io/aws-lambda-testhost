function Update-TextInFile {
    param (
        $path,
        $value
    )
    $content = Get-Content -Path $path
    $content = $content -replace "Lib", $value
    $content | Set-Content -Path $path
}

$name = $args[0]

dotnet sln remove src\Lib
dotnet sln remove test\Lib.Tests
dotnet remove test\Lib.Tests\Lib.Tests.csproj reference src\Lib\Lib.csproj

Rename-Item Lib.Sln "$name.sln"

Rename-Item src\Lib\Lib.csproj "$name.csproj"
Rename-Item src\Lib "$name"

Rename-Item test\Lib.Tests\Lib.Tests.csproj "$name.Tests.csproj"
Rename-Item test\Lib.Tests "$name.Tests"

dotnet sln add "src\$name"
dotnet sln add "test\$name.Tests"
dotnet add "test\$name.Tests\$name.Tests.csproj" reference "src\$name\$name.csproj"

Update-TextInFile "build.sh" $name.ToLower()
Update-TextInFile "build.cmd" $name.ToLower()
Update-TextInFile "build\Program.cs" $name