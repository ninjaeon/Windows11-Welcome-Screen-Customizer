# Windows 11 Welcome Screen Customizer

A Windows 11 utility that lets you customize the initial login/welcome screen background - the screen you see when starting your PC. Unlike the built-in Windows Settings which only changes the lock screen, this tool specifically modifies the welcome screen through registry modifications.

## Why This Tool?
In Windows 11, changing your lock screen background through Windows Settings only affects the lock screen you see when locking your PC (Win + L). However, it does not change the background of the initial login screen that appears when you first start your PC. Windows 11 does not provide any built-in settings to customize this initial login screen, so this tool fills that gap by providing a simple interface to change it through registry modifications.

## Features
- Select an image file through Windows Explorer
- Set the image as welcome screen background
- Prevents override by OS
- Undo changes with a single click

## Requirements
- Windows 11

## Usage
Run the application with administrative privileges:
Right-click on `WelcomeScreenCustomizer.exe` and select "Run as administrator"

## Manual Registry Modification Guide
If you prefer to make these changes manually without using the application, follow these steps:

1. Press `Win + R`, type `regedit`, and press Enter
2. When prompted by UAC, click "Yes" to run Registry Editor as administrator

3. To set the welcome screen background:
   - Navigate to: `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP`
   - If the key doesn't exist, right-click on `CurrentVersion`, select New > Key, and name it `PersonalizationCSP`
   - Create three String Values (REG_SZ) in this key:
     * Name: `LockScreenImagePath`, Value: Full path to your image (e.g., `C:\Images\background.jpg`)
     * Name: `LockScreenImageUrl`, Value: Same as LockScreenImagePath
     * Name: `LockScreenImageStatus`, Value: `1`

4. To prevent override by OS (required):
   - Navigate to: `HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Personalization`
   - If the key doesn't exist, create it
   - Create a String Value (REG_SZ):
     * Name: `LockScreenImage`, Value: Full path to your image
   - Create a DWORD (32-bit) Value:
     * Name: `NoChangingLockScreen`, Value: `1`

5. To revert changes:
   - Delete the values created in both registry locations
   - Or set `NoChangingLockScreen` to `0` to allow changes

Note: Always backup your registry before making manual modifications. Incorrect registry changes can cause system issues.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Disclaimer
This software modifies Windows registry settings. While it has been tested, use it at your own risk. Always ensure you have proper backups before modifying system settings. The authors are not responsible for any damages that might occur from using this software.
