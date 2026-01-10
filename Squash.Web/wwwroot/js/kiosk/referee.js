(function () {
    var root = document.querySelector(".screen");
    if (!root) {
        return;
    }

    var serveQuestion = document.getElementById("serveQuestion");
    var actionButtons = Array.from(document.querySelectorAll(".referee-action"));
    var serveButtons = Array.from(document.querySelectorAll(".serve-choice"));
    var eventLog = document.getElementById("eventLog");
    var serveSideInputs = Array.from(document.querySelectorAll("input[name='serveSide']"));
    var serveNames = {
        A: serveButtons[0] ? serveButtons[0].textContent.replace(" serves first", "").trim() : "Player A",
        B: serveButtons[1] ? serveButtons[1].textContent.replace(" serves first", "").trim() : "Player B"
    };

    function parseByte(value) {
        var parsed = parseInt(value, 10);
        return Number.isNaN(parsed) ? 0 : parsed;
    }

    function scoresAreZero() {
        var gameFirst = parseByte(root.dataset.gameScoreFirst);
        var gameSecond = parseByte(root.dataset.gameScoreSecond);
        var currentFirst = parseByte(root.dataset.currentGameScoreFirst);
        var currentSecond = parseByte(root.dataset.currentGameScoreSecond);
        return gameFirst === 0 && gameSecond === 0 && currentFirst === 0 && currentSecond === 0;
    }

    function setActionsDisabled(disabled) {
        actionButtons.forEach(function (button) {
            if (button instanceof HTMLAnchorElement) {
                if (disabled) {
                    button.classList.add("is-disabled");
                    button.setAttribute("aria-disabled", "true");
                    button.dataset.href = button.getAttribute("href") || "";
                    button.setAttribute("href", "#");
                } else {
                    button.classList.remove("is-disabled");
                    button.removeAttribute("aria-disabled");
                    if (button.dataset.href) {
                        button.setAttribute("href", button.dataset.href);
                    }
                }
            } else if (button instanceof HTMLButtonElement) {
                button.disabled = disabled;
                button.classList.toggle("is-disabled", disabled);
            }
        });
    }

    function setServeButtonsDisabled(disabled) {
        serveButtons.forEach(function (button) {
            button.disabled = disabled;
            button.classList.toggle("is-disabled", disabled);
        });
    }

    function showServeQuestion(show) {
        if (!serveQuestion) {
            return;
        }
        serveQuestion.style.display = show ? "grid" : "none";
    }

    function enableAfterServeChoice() {
        showServeQuestion(false);
        setActionsDisabled(false);
        setServeButtonsDisabled(false);
    }

    if (scoresAreZero()) {
        showServeQuestion(true);
        setActionsDisabled(true);
        setServeButtonsDisabled(false);
    } else {
        showServeQuestion(false);
    }

    if (serveQuestion) {
        serveQuestion.addEventListener("click", function (event) {
            var target = event.target;
            if (!(target instanceof HTMLButtonElement)) {
                return;
            }

            if (target.dataset.serve) {
                var side = serveSideInputs.find(function (input) { return input.checked; });
                var isLeft = side && side.value === "left";
                var serveEvent = null;
                if (target.dataset.serve === "A") {
                    serveEvent = isLeft ? "AServersFirstOnLeft" : "AServersFirst";
                } else if (target.dataset.serve === "B") {
                    serveEvent = isLeft ? "BServersFirstOnLeft" : "BServersFirst";
                }

                if (serveEvent) {
                    var playerName = serveNames[target.dataset.serve] || "Player";
                    var sideLabel = isLeft ? "left" : "right";
                    var label = playerName + " serves first on " + sideLabel;
                    appendLog(label);
                    logEvent(serveEvent);
                }
                enableAfterServeChoice();
            }
        });
    }

    function appendLog(text) {
        if (!eventLog) {
            return;
        }
        var item = document.createElement("div");
        item.textContent = text;
        eventLog.prepend(item);
    }

    function logEvent(eventName) {
        fetch("/api/refferee/game-log", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(eventName)
        });
    }

    actionButtons.forEach(function (button) {
        if (!(button instanceof HTMLButtonElement)) {
            return;
        }
        var eventName = button.dataset.event;
        if (!eventName) {
            return;
        }
        button.addEventListener("click", function () {
            appendLog(eventName);
            logEvent(eventName);
        });
    });
})();
