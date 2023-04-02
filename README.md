# CosmosAdvancedEnums
 Bringing Enum.ToString/TryParse to Cosmos
 
> **Note** Currently only Int32 enums are suppoorted. I do plan on adding support for the other ones at some point. 

## How to install
1) Clone the repository and build it or download the latest release if available
2) Add the build output folder to your PATH env variable or similiar

## How to use with a project
1) Make sure to restart Visual Studio after changing your PATH variable
2) Right click your project and then click on "Properties"
3) On the left side, open "Build" and then click "Events"
4) Add following line to the post-build event: `CosmosAdvancedEnums bin\Debug\net6.0\<YourProjectName>.dll`
5) You should now be able to use Enum.ToString/FromString with your Cosmos OS


## External libraries
> **Warning**<br>
> The post processor only applies to the project you run it on! To use external libraries, follow below instructions

### From imported project
If you are using an imported project and therefore rebuild the DLL when required anyways, apply the same instructions as in "How to use with a project" to the class library project. Rebuild the project afterwards and then rebuild your os.

### From imported DLL
You are gonna have to manually run the post-processor everytime you change that DLL. To do so, just open the cmd or shell in the folder of the DLL, then enter "CosmosAdvancedEnums <dll file>".

## Credits
CosmosAdvancedEnums uses Mono.Cecil (licensed under MIT) to modify and analyze the IL of the dlls
