import * as signalR from "@microsoft/signalr"

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();
