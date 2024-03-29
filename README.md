# gvrocksnow's PixelArt Upscaler
Upscales pixel art while keeping clean single-pixel outlines. Generates some artifacts which can be avoided with proper pixel placement. Written in Unity 2019.3.0f6.

**Download Executable** - https://gvrocksnow.itch.io/pixelart-upscaler<br /><br />

![Sheet](https://user-images.githubusercontent.com/22365275/68945399-56454f00-07d6-11ea-9bb0-ffc697e3e4d8.png)<br>
**Original Art** (Credit - https://alexs-assets.itch.io/16x16-rpg-item-pack)<br /><br />


![Sheet_2X](https://user-images.githubusercontent.com/22365275/68945447-72e18700-07d6-11ea-9001-7d9fe6ded36e.png)<br>
**2X Upscaling**<br /><br />


![Sheet_4X](https://user-images.githubusercontent.com/22365275/68945477-7bd25880-07d6-11ea-8b37-32b8aff9d145.png)<br>
**4X Upscaling**<br /><br /><br />


# USAGE
![Capture](https://user-images.githubusercontent.com/22365275/167245256-6853ef97-46c3-4fd2-a8ff-224e9e7f2300.png)

1. Select the outline color of the input images. (Default is black)
2. Enter a scale factor amount. (Ideally between 2-6)
3. Select the input folder for multiple files or input file for single file. (.png format)
4. Output files are exported in the same folder as input files.

# OPTIONS

- **Output Suffix** : Appends this string to the output file name.  
- **Remove Extra Lines** : Removes extra lines when generating outlines.  
- **Fill Gaps** : Fill blocks surrounded by outlines.  
- **Try Fixing Artifacts** : Experimental feature to fix artifacts when generating outlines. Recommended to leave this off.  
- **Fill Colors** : Fills the outlines with colors.  
- **Interpolate Fill Colors** : Interpolates the fill colors from the previous step.  
- **Interpolation Order** : Specifies whether dark colors are drawn on top of light colors or vice versa. Can be set to "light colors over dark" if the reverse order is needed.


**Credits for plugins used:**    
**Color Picker** - https://github.com/judah4/HSV-Color-Picker-Unity     
**Standalone File Browser** - https://github.com/gkngkc/UnityStandaloneFileBrowser 
