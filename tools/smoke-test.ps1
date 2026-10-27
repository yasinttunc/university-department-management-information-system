<#
.SYNOPSIS
  Starts the built desktop app against a fresh database, signs in as each demo user through UI Automation,
  checks the role-based menu and saves screenshots. Used by CI on windows-latest.
#>
param(
    [Parameter(Mandatory = $true)] [string] $Exe,
    [string] $OutDir = "smoke",
    # Capture only the app's active window (maximised) instead of the whole screen, for README screenshots.
    [switch] $WindowOnly
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
$AE = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$dbDir = Join-Path $env:RUNNER_TEMP "udmis-smoke"
New-Item -ItemType Directory -Force -Path $dbDir | Out-Null
$env:UDMIS_DB_PATH = Join-Path $dbDir "university.db"

Add-Type -Namespace Smoke -Name Native -MemberDefinition @'
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Rect { public int Left; public int Top; public int Right; public int Bottom; }
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern System.IntPtr GetForegroundWindow();
[System.Runtime.InteropServices.DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(System.IntPtr hwnd, int attribute, out Rect rect, int size);
'@

function Save-Screenshot([string] $Name) {
    $bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    if ($WindowOnly) {
        $rect = New-Object Smoke.Native+Rect
        # DWMWA_EXTENDED_FRAME_BOUNDS (9): the visible window without its drop shadow.
        if ([Smoke.Native]::DwmGetWindowAttribute([Smoke.Native]::GetForegroundWindow(), 9, [ref] $rect, 16) -eq 0) {
            $bounds = New-Object System.Drawing.Rectangle $rect.Left, $rect.Top, ($rect.Right - $rect.Left), ($rect.Bottom - $rect.Top)
        }
    }
    $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($bounds.Location, [System.Drawing.Point]::Empty, $bounds.Size)
    $bitmap.Save((Join-Path $OutDir "$Name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose(); $bitmap.Dispose()
}

# Finds a window by title: top-level windows first, then windows owned by the app's windows
# (UI Automation lists an owned dialog under its owner, not under the desktop).
function Find-Window([int] $ProcessId, [string] $Title) {
    $byName = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $Title)
    $byProcess = New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $ProcessId)
    $isWindow = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)

    $window = $AE::RootElement.FindFirst($Scope::Children, (New-Object System.Windows.Automation.AndCondition($byName, $isWindow)))
    if ($window) { return $window }

    foreach ($top in $AE::RootElement.FindAll($Scope::Children, $byProcess)) {
        $owned = $top.FindFirst($Scope::Descendants, (New-Object System.Windows.Automation.AndCondition($byName, $isWindow)))
        if ($owned) { return $owned }
    }
    return $null
}

function Wait-Window([System.Diagnostics.Process] $Process, [string] $Title, [int] $Seconds = 60) {
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) {
            $log = Join-Path $dbDir "error.log"
            if (Test-Path $log) { Get-Content $log | Write-Host }
            throw "The app exited with code $($Process.ExitCode) while waiting for '$Title'."
        }
        $window = Find-Window $Process.Id $Title
        if ($window) { return $window }
        Assert-NoErrorDialog
        Start-Sleep -Milliseconds 500
    }
    Save-Screenshot "timeout"
    throw "Timed out waiting for the window '$Title'."
}

function Find-ByName($Parent, [string] $Name) {
    $element = $Parent.FindFirst($Scope::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $Name)))
    if (-not $element) { throw "Element '$Name' not found." }
    return $element
}

function Set-Text($Element, [string] $Value) {
    $Element.SetFocus()
    $pattern = $null
    if ($Element.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref] $pattern)) {
        $pattern.SetValue($Value)
    } else {
        [System.Windows.Forms.SendKeys]::SendWait($Value)
    }
}

function Sign-In($Process, [string] $User, [string] $Password) {
    $login = Wait-Window $Process "Sign in - University Department MIS"
    Set-Text (Find-ByName $login "Username") $User
    Set-Text (Find-ByName $login "Password") $Password
    $button = Find-ByName $login "SIGN IN"
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    return Wait-Window $Process "University Department MIS"
}

function Assert-NoErrorDialog {
    $errorDialog = Find-Window $process.Id "University Department MIS - error"
    if ($errorDialog) {
        Save-Screenshot "error"
        $log = Join-Path $dbDir "error.log"
        if (Test-Path $log) { Get-Content $log | Write-Host }
        throw "The app showed an error dialog."
    }
}

function Open-Screen($Main, [string] $Title, [string] $Shot) {
    $item = Find-ByName (Find-ByName $Main "Main menu") $Title
    $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Start-Sleep -Seconds 2
    Assert-NoErrorDialog
    Save-Screenshot $Shot
    Write-Host "Opened screen: $Title"
    $script:step = "after opening $Title"
}

function Grid-RowCount($Main, [string] $AutomationId) {
    $grid = $Main.FindFirst($Scope::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition($AE::AutomationIdProperty, $AutomationId)))
    if (-not $grid) { throw "Grid '$AutomationId' not found." }
    return $grid.GetCurrentPattern([System.Windows.Automation.GridPattern]::Pattern).Current.RowCount
}

function Maximize($Window) {
    if ($WindowOnly) {
        $Window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).SetWindowVisualState(
            [System.Windows.Automation.WindowVisualState]::Maximized)
        Start-Sleep -Seconds 1
    }
}

function Select-ComboItem($Main, [string] $ComboName, [string] $ItemName) {
    $combo = Find-ByName $Main $ComboName
    $expand = $combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $expand.Expand()
    Start-Sleep -Milliseconds 500
    (Find-ByName $combo $ItemName).GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    $expand.Collapse()
    Start-Sleep -Seconds 1
}

function Menu-Names($Main) {
    $menu = Find-ByName $Main "Main menu"
    $items = $menu.FindAll($Scope::Children,
        (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)))
    return @($items | ForEach-Object { $_.Current.Name })
}

$process = Start-Process -FilePath $Exe -PassThru
$step = "starting the app"
try {
    $login = Wait-Window $process "Sign in - University Department MIS"
    Start-Sleep -Seconds 1
    Save-Screenshot "01-sign-in"

    # A wrong password must keep the user on the sign-in window with a message.
    Set-Text (Find-ByName $login "Username") "admin"
    Set-Text (Find-ByName $login "Password") "wrong-password"
    (Find-ByName $login "SIGN IN").GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Seconds 3
    Find-ByName $login "Sign-in error" | Out-Null
    Save-Screenshot "02-wrong-password"
    Write-Host "Wrong password shows the sign-in error: OK"

    $step = "signing in as admin"
    $main = Sign-In $process "admin" "Admin2026"
    Maximize $main
    Start-Sleep -Seconds 1
    Save-Screenshot "03-admin-menu"
    $adminMenu = Menu-Names $main
    Write-Host "Admin menu: $($adminMenu -join ', ')"
    if ($adminMenu -notcontains "Users" -or $adminMenu -notcontains "Data import") { throw "Admin menu is missing admin screens." }

    $step = "opening Students"
    Open-Screen $main "Students" "05-students"
    $rows = Grid-RowCount $main "StudentsGrid"
    if ($rows -ne 65) { throw "Students grid shows $rows rows, expected 65." }
    $step = "opening the new-student dialog"
    (Find-ByName $main "ADD STUDENT").GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $dialog = Wait-Window $process "New student" 20
    Start-Sleep -Seconds 1
    Save-Screenshot "06-new-student"
    (Find-ByName $dialog "CANCEL").GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Seconds 1

    Open-Screen $main "Staff" "07-staff"
    $rows = Grid-RowCount $main "StaffGrid"
    if ($rows -ne 13) { throw "Staff grid shows $rows rows, expected 13." }
    Open-Screen $main "Courses & sections" "08-courses"
    Open-Screen $main "Timetable" "09-timetable"
    Select-ComboItem $main "Lecturer" "Prof. Erdal Toprak"
    Save-Screenshot "09-timetable"
    Open-Screen $main "Advising" "10-advising"
    Select-ComboItem $main "Advisor" "Assoc. Prof. Derya Karaca"
    Save-Screenshot "10-advising"
    Open-Screen $main "Calendar" "11-calendar"
    if ($WindowOnly) {
        # For the README: show next month and pick a day that has an event, if there is one.
        (Find-ByName $main "Next month").GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        Start-Sleep -Seconds 1
        $days = $main.FindAll($Scope::Descendants, (New-Object System.Windows.Automation.PropertyCondition(
            $AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)))
        $eventDay = @($days | Where-Object { $_.Current.Name -like "*event(s)" }) | Select-Object -Last 1
        if ($eventDay) {
            $eventDay.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
            Start-Sleep -Seconds 1
        }
        Save-Screenshot "11-calendar"
    }
    Open-Screen $main "Reminders" "12-reminders"
    Open-Screen $main "Mail" "13-mail"
    $rows = Grid-RowCount $main "RecipientsGrid"
    if ($rows -ne 65) { throw "Mail recipients grid shows $rows rows, expected 65." }
    Open-Screen $main "Data import" "14-data-import"
    Open-Screen $main "Users" "15-users"
    $rows = Grid-RowCount $main "UsersGrid"
    if ($rows -ne 2) { throw "Users grid shows $rows rows, expected 2." }
    Open-Screen $main "Dashboard" "16-dashboard"
    $screens = 11

    (Find-ByName $main "SIGN OUT").GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $step = "signing in as staff"
    $main = Sign-In $process "staff" "Staff2026"
    Start-Sleep -Seconds 1
    Save-Screenshot "17-staff-menu"
    $staffMenu = Menu-Names $main
    Write-Host "Staff menu: $($staffMenu -join ', ')"
    if ($staffMenu -contains "Users" -or $staffMenu -contains "Data import") { throw "Staff menu shows admin screens." }

    Write-Host "::notice title=UI smoke test::Sign-in window, wrong-password message, admin menu ($($adminMenu.Count) items), $screens screens, the new-student dialog and staff menu ($($staffMenu.Count) items) all checked."
}
catch {
    $log = Join-Path $dbDir "error.log"
    $details = if (Test-Path $log) { (Get-Content $log -Raw) -replace "`r?`n", " | " } else { "no error.log" }
    Write-Host "::error title=UI smoke test::Failed $step. $($_.Exception.Message) App log: $details"
    throw
}
finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
}
