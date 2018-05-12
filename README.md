# NZSmartIDReader
Vision API to identify if an image is of a New Zealand's driver license. Built on top of Azure Custom Vision API.

_This is just an experimental project_

## Using this project

* Clone this repository to your local machine
* Make you sure you have Visual Studio 2017 or greater installed
* You need a Custom Vision API account. If you don't have one, create one on this url: https://customvision.ai/
* Update the `appsettings.json` file under `NZSmartIDReader.Api` project and add your Training and Prediction keys from Custom Vision
* Upload images to train the model under `NZSmartIDReader.Api/Images/Global` and `NZSmartIDReader.Api/Images/NewZealand`
* Run the project
