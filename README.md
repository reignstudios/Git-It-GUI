# Git-It-GUI
Download: https://github.com/reignstudios/Git-It-GUI/releases

![alt tag](ScreenShots/Changes.png?raw=true)


- Invokes git commands directly allowing any filters to work (including git-lfs).

- Writen in C# / WPF (will support macOS and Linux/BSD in the future with Xamarin.Forms 3.0).

- Auto check for .gitignore file.

- Auto check for Git-LFS install.

- Doesn't allow non resolved files to be staged without warning.

- Supports Meld, kDiff3, P4Merge and DiffMerge with no .gitconfig required.

- Auto checks for Git and Git-LFS min-version requirements.

- gitk for history (maybe something built in later).

LOGS: These will be stored in "C:\ProgramData\GitItGUI\logs.txt" on windows

# NOTE: If you can use 'git / git lfs' in the terminal/cmd this tool wont work.
 - Win32: https://git-scm.com/
<!-- - macOS (recommend homebrew):
    - Install git via homebrew: "brew install git" and "brew install git-lfs"
    - Set "VS for Mac" Enviroment var in proj settings: "PATH" = "/usr/local/bin"
 - Linux:
     - Install git via terminal-->