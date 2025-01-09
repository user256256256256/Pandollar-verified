// Setup SignalR Connection
const connection = new signalR.HubConnectionBuilder()
.withUrl("/notificationHub")
.configureLogging(signalR.LogLevel.Information)
.build();

connection.on("ReceiveNotification", function (type, message) {
    console.log("[Client] Notification received - Type: " + type + ", Message: " + message);
    showToastNotification(type, message);
});

connection.start()
    .then(() => console.log("[Client] SignalR connection established"))
    .catch(err => console.error("[Client] SignalR connection error: " + err.toString()));

connection.onclose(() => {
    console.log("[Client] SignalR connection closed. Attempting to reconnect...");
    setTimeout(() => connection.start().then(() => console.log("[Client] SignalR reconnected")), 5000);
});

function showToastNotification(type, message) {
    console.log("[Client] Showing toast notification - Type: " + type + ", Message: " + message);

    let toastType;
    switch (type) {
        case 'success':
            toastType = 'success';
            break;
        case 'info':
            toastType = 'info';
            break;
        case 'warning':
            toastType = 'warning';
            break;
        case 'error':
            toastType = 'error';
            break;
        default:
            toastType = 'info';
    }

    $("#toastContainer").dxToast({
        message: message,
        type: toastType,
        displayTime: 10000, // Set to 0 to make it stay indefinitely until closed
        closeOnClick: true, // Allow users to close by clicking on the notification
        hideOnOutsideClick: false, // Allow users to close by clicking outside the notification
        position: {
            my: 'center top',
            at: 'center top',
            of: window
        },
        onShown: function () {
            console.log(`[Client] Toast notification displayed - Type: ${toastType}, Message: ${message}`);
        }
    }).dxToast("show");
}

