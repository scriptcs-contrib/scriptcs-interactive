# scriptcs-interactive
Use scriptcs from within the C# Interactive Window in Visual Studio

![ScreenShot](https://raw.github.com/scriptcs-contrib/scriptcs-interactive/images/interactive1.png)

## Features
* Load and execute existing scriptcs csx files, including those that depend on script packs.
* Directly execute loose scriptcs code.
* Return objects created within scriptcs back to the interactive where they can then be used within your interactive scripts
* Directly load script packs.

## Requirements
* Visual Studio 2015 Update 1
* scriptcs 0.15.0 or higher

## Installation
* Install scriptcs or svm.
* Clone this repo and copy `scriptcs.csx` into your home directory (i.e. c:\Users\gblock)
* Edit `scriptcs.csx` and modify the #r's at the top to point to the location of your scriptcs version. By default it points to the latest scriptcs release. 

## Usage
### Loading scriptcs interactive
* Open Visual Studio 2015
* In the C# Interactive Window type `#load "scriptcs.csx"`


