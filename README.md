# ReverseGeocoding
Idea behind this project is to have all cities and countries where pictures were made in a database. In order to display them later on a map, since there are too much of pictures and loading JSON with saved locations takes too much time, like this number of locations will be filtered per country, then per city, and per number of locations depending on zoom and user decision.

Example of JSON configuration file:

{
  "gapikey": "googleAPIkey",
  "jsons": "C:\\projects\\gallery\\folderE\\folderE.json;C:\\projects\\gallery\\folderF\\folderF.json;C:\\projects\\gallery\\folderG\\folderG.json",
  "connectionString": "Server=myServer;Database=ReverseGeocoding;Uid=myUserID;Pwd=myPassword;"
}

Where JSON files (jsons) are generated with Pics2gMaps project, using "mergedGalleries" configuration settings.