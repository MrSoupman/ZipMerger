# ZipMerger
Merges zip files downloaded from Imgur into one single zip, renaming each file into the format xxxx.y where x is a leading zero, and y is the file format. While made specifically for imgur albums, the program will work for any zip archive as long as the files follow some "x -" format, where x is some number. The dash in the file name is a requirement.
 
# Requirements
* At least Windows 10 (Untested on other versions)
* [.NET Core >= 3.1](https://dotnet.microsoft.com/download/dotnet/3.1/runtime)

# Usage
* Add each zip file using the Add button.
* The order that each zip is merged depends on the order in the list. Click on a zip and use either the move up or move down button to change the order.
* Enter a name for the new zip file. This automatically chooses the first zip archive that was added. If ".zip" is not added at the end, it will be added automatically upon clicking merge.
* Choose a directory to save the newly merged zip file by clicking the browse button.
* Finally, click merge.

# License
This program is licensed under GNU GPL v3.0. A copy of the license is included in each release as well as in the repository itself.