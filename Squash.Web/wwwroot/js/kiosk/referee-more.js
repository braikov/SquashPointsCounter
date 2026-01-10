(function () {
    var actionButtons = Array.from(document.querySelectorAll(".referee-action"));
    if (!actionButtons.length) {
        return;
    }

    function logEvent(eventName) {
        fetch("/api/refferee/game-log", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(eventName)
        });
    }

    actionButtons.forEach(function (button) {
        var eventName = button.dataset.event;
        if (!eventName) {
            return;
        }

        button.addEventListener("click", function () {
            logEvent(eventName);
        });
    });
})();
