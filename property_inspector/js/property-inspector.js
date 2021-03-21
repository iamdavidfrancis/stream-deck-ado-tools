// global websocket, used to communicate from/to Stream Deck software
// as well as some info about our plugin, as sent by Stream Deck software 
var websocket = null,
    uuid = null,
    inInfo = null,
    actionInfo = {},
    settingsModel = {
        Organization: '',
        Project: '',
        Username: '',
        PAT: '',
        DefinitionId: 0,
        EnvironmentId: 0
    };

function connectElgatoStreamDeckSocket(inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) {
    uuid = inUUID;
    actionInfo = JSON.parse(inActionInfo);
    inInfo = JSON.parse(inInfo);
    websocket = new WebSocket('ws://localhost:' + inPort);

    //initialize values
    if (actionInfo.payload.settings.settingsModel) {
        settingsModel.Organization = actionInfo.payload.settings.settingsModel.Organization;
        settingsModel.Project = actionInfo.payload.settings.settingsModel.Project;
        settingsModel.Username = actionInfo.payload.settings.settingsModel.Username;
        settingsModel.PAT = actionInfo.payload.settings.settingsModel.PAT;
        settingsModel.DefinitionId = actionInfo.payload.settings.settingsModel.DefinitionId;
        settingsModel.EnvironmentId = actionInfo.payload.settings.settingsModel.EnvironmentId;
    }

    document.getElementById('txtOrganizationValue').value = settingsModel.Organization;
    document.getElementById('txtProjectValue').value = settingsModel.Project;
    document.getElementById('txtUserValue').value = settingsModel.Username;
    document.getElementById('txtPATValue').value = settingsModel.PAT;
    document.getElementById('txtReleaseDefValue').value = settingsModel.DefinitionId;
    document.getElementById('txtEnvDefValue').value = settingsModel.EnvironmentId;

    websocket.onopen = function () {
        var json = { event: inRegisterEvent, uuid: inUUID };
        // register property inspector to Stream Deck
        websocket.send(JSON.stringify(json));

    };

    websocket.onmessage = function (evt) {
        // Received message from Stream Deck
        var jsonObj = JSON.parse(evt.data);
        var sdEvent = jsonObj['event'];
        switch (sdEvent) {
            case "didReceiveSettings":
                if (jsonObj.payload.settings.settingsModel.Organization) {
                    settingsModel.Organization = jsonObj.payload.settings.settingsModel.Organization;
                    document.getElementById('txtOrganizationValue').value = settingsModel.Organization;
                } else if (jsonObj.payload.settings.settingsModel.Project) {
                    settingsModel.Project = jsonObj.payload.settings.settingsModel.Project;
                    document.getElementById('txtProjectValue').value = settingsModel.Project;
                } else if (jsonObj.payload.settings.settingsModel.Username) {
                    settingsModel.Username = jsonObj.payload.settings.settingsModel.Username;
                    document.getElementById('txtUserValue').value = settingsModel.Username;
                } else if (jsonObj.payload.settings.settingsModel.PAT) {
                    settingsModel.PAT = jsonObj.payload.settings.settingsModel.PAT;
                    document.getElementById('txtPATValue').value = settingsModel.PAT;
                } else if (jsonObj.payload.settings.settingsModel.DefinitionId) {
                    settingsModel.DefinitionId = jsonObj.payload.settings.settingsModel.DefinitionId;
                    document.getElementById('txtReleaseDefValue').value = settingsModel.DefinitionId;
                } else if (jsonObj.payload.settings.settingsModel.EnvironmentId) {
                    settingsModel.EnvironmentId = jsonObj.payload.settings.settingsModel.EnvironmentId;
                    document.getElementById('txtEnvDefValue').value = settingsModel.EnvironmentId;
                } 
                break;
            default:
                break;
        }
    };
}

const setSettings = (value, param) => {
    if (websocket) {
        settingsModel[param] = value;
        var json = {
            "event": "setSettings",
            "context": uuid,
            "payload": {
                "settingsModel": settingsModel
            }
        };
        websocket.send(JSON.stringify(json));
    }
};

