# gvrocksnow's PixelArt Upscaler
Upscales pixel art while keeping clean single-pixel outlines. Generates some artifacts which can be avoided with proper pixel placement.

Download Executable - https://gvrocksnow.itch.io/pixelart-upscaler

Original Art (Credit - https://alexs-assets.itch.io/16x16-rpg-item-pack)

![Sheet](https://user-images.githubusercontent.com/22365275/68945399-56454f00-07d6-11ea-9bb0-ffc697e3e4d8.png)

2X Upscaling

![Sheet_2X](https://user-images.githubusercontent.com/22365275/68945447-72e18700-07d6-11ea-9001-7d9fe6ded36e.png)

4X Upscaling

![Sheet_4X](https://user-images.githubusercontent.com/22365275/68945477-7bd25880-07d6-11ea-8b37-32b8aff9d145.png)
 

# USAGE
![Capture](https://user-images.githubusercontent.com/22365275/68947245-01f09e00-07db-11ea-9e57-9e7f4d7fa45d.PNG)  

1. Select an outline color. (Default is black)
2. Enter a scale factor amount. (Ideally between 2-6)
3. Select the input folder for multiple files or input file for single file (.png format)
4. Output files are located in same folder as input files

# OPTIONS

- Output suffix : Appends this string to the output file.  
- Remove extra lines : Removes extra lines when generating outlines.  
- Fill gaps : Fill blocks surrounded by outlines.  
- Try fixing artifacts : Experimental feature to fix artifacts when generating outlines. Recommended to leave this off.  
- Fill Colors : Fills the outlines with colors.  
- Interpolate Fill Colors : Interpolates the fill colors from the previous step.  
- Interpolation Order : Specifies whether dark colors are drawn on top of light colors or vice versa. Can be set to "light colors over dark" if the reverse order is needed.


Credits for plugins used:  
Color Picker - https://github.com/judah4/HSV-Color-Picker-Unity    
Standalone File Browser - https://github.com/gkngkc/UnityStandaloneFileBrowser 
