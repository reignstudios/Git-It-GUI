# Git-It-GUI
Download: https://github.com/reignstudios/Git-It-GUI/releases

![alt tag](ScreenShots/Changes.png?raw=true)


- Invokes git commands directly allowing any filters to work (including git-lfs).

- Writen in C# / WPF (will maybe support macOS & Linux/BSD in the future but UI change needed).

- Image Diff preview.

- Colorized text diff preview.

- Auto check for .gitignore file.

- Auto check for Git-LFS install.

- Doesn't allow non resolved files to be staged without warning.

- Supports Meld, kDiff3, P4Merge and DiffMerge with no .gitconfig required.

- Auto checks for Git and Git-LFS min-version requirements.

- gitk for history (maybe something built in later).

---
LOGS: These will be stored in "C:\ProgramData\GitItGUI\logs.txt" on windows

---
## Requirements: git-core, git-lfs and .NET 4.8
### (git v2.11.0) and (git-lfs v1.5.5) or later are required!
 - Win7-Win10: https://git-scm.com/
<!-- - macOS (recommend homebrew):
    - Install git via homebrew: "brew install git" and "brew install git-lfs"
    - Set "VS for Mac" Enviroment var in proj settings: "PATH" = "/usr/local/bin"
 - Linux:
     - Install git via terminal-->
#### To view single page PDF diffs download and install: https://www.ghostscript.com/download/gsdnld.html