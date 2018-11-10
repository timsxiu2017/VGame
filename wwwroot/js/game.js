"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/hubs/game?access_token="+_token).build();
var content = document.getElementById("messagesList");
function addLi(msg,color)
{
    var li = document.createElement("li");
    li.textContent = msg;
    li.style.color = color;
    if (content.firstChild==null) content.appendChild(li);
    else content.insertBefore(li,content.firstChild);
}

connection.on("FD",function(msg){
    addLi(msg,"#090");
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});