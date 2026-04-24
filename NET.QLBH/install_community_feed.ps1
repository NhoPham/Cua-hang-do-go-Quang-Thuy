$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Get-Location

Write-Host "Dang cai module Cong dong do go vao: $project" -ForegroundColor Cyan

$folders = @("Controllers", "Services", "ViewModels", "Views", "wwwroot")
foreach ($folder in $folders) {
    $source = Join-Path $root $folder
    if (Test-Path $source) {
        Copy-Item $source -Destination $project -Recurse -Force
    }
}

$programPath = Join-Path $project "Program.cs"
if (Test-Path $programPath) {
    $program = Get-Content $programPath -Raw
    if ($program -notmatch "ICommunityNewsService") {
        $registration = 'builder.Services.AddHttpClient<ICommunityNewsService, CommunityNewsService>(client => { client.Timeout = TimeSpan.FromSeconds(20); }); builder.Services.AddScoped<ICommunityFeedService, CommunityFeedService>(); '
        if ($program -match "builder\.Services\.AddAuthentication") {
            $program = $program -replace "builder\.Services\.AddAuthentication", ($registration + "builder.Services.AddAuthentication")
        }
        elseif ($program -match "builder\.Services\.AddAuthorization\(\);") {
            $program = $program -replace "builder\.Services\.AddAuthorization\(\);", ("builder.Services.AddAuthorization(); " + $registration)
        }
        else {
            $program = $program -replace "var app = builder\.Build\(\);", ($registration + "var app = builder.Build();")
        }
        Set-Content $programPath $program -Encoding UTF8
        Write-Host "Da them service vao Program.cs" -ForegroundColor Green
    }
    else {
        Write-Host "Program.cs da co Community services, bo qua." -ForegroundColor Yellow
    }
}

$layoutPath = Join-Path $project "Views\Shared\_Layout.cshtml"
if (Test-Path $layoutPath) {
    $layout = Get-Content $layoutPath -Raw
    if ($layout -notmatch "asp-controller=\"Community\"") {
        $navLink = '<a class="qt-nav-link @(currentController == "Community" ? "active" : "")" asp-controller="Community" asp-action="Index">Cộng đồng</a>'
        $updated = $layout -replace '(<a[^>]*asp-controller="Product"[^>]*asp-action="Index"[^>]*>.*?</a>)', "`$1`r`n            $navLink"
        if ($updated -ne $layout) {
            Set-Content $layoutPath $updated -Encoding UTF8
            Write-Host "Da them menu Cong dong vao _Layout.cshtml" -ForegroundColor Green
        }
        else {
            Write-Host "Chua tu dong them duoc menu. Hay them thu cong link /Community vao _Layout.cshtml" -ForegroundColor Yellow
        }
    }
}

Write-Host "Hoan tat. Hay chay: dotnet build" -ForegroundColor Cyan
