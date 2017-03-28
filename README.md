# Git-It-GUI

![alt tag](ScreenShots/Changes.png?raw=true)


- Invokes git commands directly allowing any filters to work (including git-lfs).

- Is writen in C# / AvaloniaUI (should support macOS and Linux/UNIX in the future).

- Auto check for .gitignore file.

- Auto check for Git-LFS install.

- Doesn't allow non resolved files to be staged without warning.

- Supports Meld, kDiff3, P4Merge and DiffMerge with no .gitconfig required.

- Auto checks for Git and Git-LFS min-version requirements.

- gitk for history (maybe something built in later).

Note: This is built using the nightly builds of Avalonia: https://github.com/AvaloniaUI/Avalonia/wiki/Using-nightly-build-feed

LOGS: These will be stored in "C:\ProgramData\GitItGUI\logs.txt" on windows

macOS NOTE: To debug you need to:
    - Install git via homebrew: "brew install git" and "brew install git-lfs"
    - Set "VS for Mac" Enviroment var in proj settings: "PATH" = "/usr/local/bin"
