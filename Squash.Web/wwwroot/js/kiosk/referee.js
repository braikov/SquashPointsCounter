(function () {
    var root = document.querySelector(".screen");
    if (!root) {
        return;
    }

    var serveQuestion = document.getElementById("serveQuestion");
    var actionButtons = Array.from(document.querySelectorAll(".referee-action"));
    var serveButtons = Array.from(document.querySelectorAll(".serve-choice"));
    var eventLog = document.getElementById("eventLog");
    var serveStatus = document.getElementById("serveStatus");
    var serveStatusText = document.getElementById("serveStatusText");
    var serveSideToggle = document.getElementById("serveSideToggle");
    var serveOnLeft = document.getElementById("serveOnLeft");
    var serveNames = {
        A: serveButtons[0] ? serveButtons[0].textContent.replace(" serves first", "").trim() : "Player A",
        B: serveButtons[1] ? serveButtons[1].textContent.replace(" serves first", "").trim() : "Player B"
    };

    var matchState = {
        gameScoreFirst: parseByte(root.dataset.gameScoreFirst),
        gameScoreSecond: parseByte(root.dataset.gameScoreSecond),
        currentGameScoreFirst: parseByte(root.dataset.currentGameScoreFirst),
        currentGameScoreSecond: parseByte(root.dataset.currentGameScoreSecond),
        lastPointWinner: null,
        currentServer: null,
        serveSide: "right",
        initialServeChosen: false,
        awaitingSideChoice: false
    };

    var eventHistory = [];

    function parseByte(value) {
        var parsed = parseInt(value, 10);
        return Number.isNaN(parsed) ? 0 : parsed;
    }

    function scoresAreZero() {
        return matchState.gameScoreFirst === 0
            && matchState.gameScoreSecond === 0
            && matchState.currentGameScoreFirst === 0
            && matchState.currentGameScoreSecond === 0;
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

    function showServeButtons(show) {
        serveButtons.forEach(function (button) {
            button.style.display = show ? "block" : "none";
        });
    }

    function updateServeStatus() {
        if (!serveStatus || !serveStatusText || !matchState.currentServer) {
            return;
        }
        var name = serveNames[matchState.currentServer] || matchState.currentServer;
        serveStatusText.textContent = name + " serves on " + matchState.serveSide;
        serveStatus.style.display = "block";
    }

    function showServeStatus(show) {
        if (!serveStatus) {
            return;
        }
        serveStatus.style.display = show ? "block" : "none";
    }

    function showServeSideToggle(show) {
        if (!serveSideToggle) {
            return;
        }
        serveSideToggle.style.display = show ? "inline-flex" : "none";
    }

    function enableAfterServeChoice() {
        showServeQuestion(false);
        setActionsDisabled(false);
        setServeButtonsDisabled(false);
        showServeStatus(true);
        showServeSideToggle(false);
        matchState.awaitingSideChoice = false;
    }

    function setServeSide(isLeft) {
        matchState.serveSide = isLeft ? "left" : "right";
        if (serveOnLeft) {
            serveOnLeft.checked = isLeft;
        }
    }

    function appendLog(text) {
        if (!eventLog) {
            return;
        }
        var item = document.createElement("div");
        item.textContent = text;
        eventLog.prepend(item);
    }

    function sendEvent(eventName) {
        fetch("/api/refferee/game-log", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(eventName)
        });
    }

    function formatLog(eventName, labelOverride) {
        if (labelOverride) {
            return labelOverride;
        }
        return eventName;
    }

    function applyEvent(eventName) {
        var needsPointA = eventName === "PointA" || eventName === "StrokeA" || eventName === "ConductStrokeA";
        var needsPointB = eventName === "PointB" || eventName === "StrokeB" || eventName === "ConductStrokeB";

        if (needsPointA) {
            matchState.currentGameScoreFirst += 1;
        }
        if (needsPointB) {
            matchState.currentGameScoreSecond += 1;
        }

        if (needsPointA || needsPointB) {
            var currentWinner = needsPointA ? "A" : "B";
            var isHandout = matchState.lastPointWinner && matchState.lastPointWinner !== currentWinner;

            if (isHandout) {
                matchState.currentServer = currentWinner;
                showServeQuestion(false);
                showServeButtons(false);
                showServeStatus(true);
                setServeSide(false);
                showServeSideToggle(true);
                matchState.awaitingSideChoice = true;
            } else {
                matchState.awaitingSideChoice = false;
                showServeSideToggle(false);
            }

            matchState.lastPointWinner = currentWinner;
            if (!isHandout && matchState.currentServer === currentWinner) {
                matchState.serveSide = matchState.serveSide === "left" ? "right" : "left";
            }
            updateServeStatus();
            updateScoreDisplay();

            if (isGameOver()) {
                var winner = matchState.currentGameScoreFirst > matchState.currentGameScoreSecond ? "A" : "B";
                applyEndOfGame(winner);
            }
        }

        if (eventName === "ARetires") {
            if (confirm("A retires. B wins the match. Continue?")) {
                window.location.href = "/m";
            }
            return;
        }

        if (eventName === "BRetires") {
            if (confirm("B retires. A wins the match. Continue?")) {
                window.location.href = "/m";
            }
            return;
        }

        if (eventName === "InjuryTimeoutA" || eventName === "InjuryTimeoutB") {
            return;
        }

        if (eventName === "EquipmentIssue") {
            return;
        }

        if (needsPointA || needsPointB) {
            // Placeholder: add game-end logic later.
        }
    }

    function isGameOver() {
        var a = matchState.currentGameScoreFirst;
        var b = matchState.currentGameScoreSecond;
        var maxScore = Math.max(a, b);
        var diff = Math.abs(a - b);
        return maxScore >= 11 && diff >= 2;
    }

    function applyEndOfGame(winner) {
        if (winner === "A") {
            matchState.gameScoreFirst += 1;
        } else {
            matchState.gameScoreSecond += 1;
        }

        matchState.currentGameScoreFirst = 0;
        matchState.currentGameScoreSecond = 0;
        matchState.lastPointWinner = winner;
        matchState.currentServer = winner;
        setServeSide(false);
        showServeSideToggle(false);
        updateServeStatus();
        updateScoreDisplay();

        logDerivedEvent("EndGame", "Game to " + (serveNames[winner] || winner));

        if (matchState.gameScoreFirst >= 3 || matchState.gameScoreSecond >= 3) {
            logDerivedEvent("EndMatch", "Match won by " + (serveNames[winner] || winner));
        }
    }

    function logDerivedEvent(eventName, labelOverride) {
        eventHistory.push(eventName);
        appendLog(formatLog(eventName, labelOverride));
        sendEvent(eventName);
    }

    function updateScoreDisplay() {
        var matchPoints = document.getElementById("matchPoints")
            || document.querySelector(".match-header__match-score");
        if (matchPoints) {
            if (!matchPoints.id) {
                matchPoints.id = "matchPoints";
            }
            matchPoints.textContent = matchState.currentGameScoreFirst + ":" + matchState.currentGameScoreSecond;
        }

        var matchGames = document.getElementById("matchGames")
            || document.querySelector(".match-header__separator");
        if (matchGames) {
            if (!matchGames.id) {
                matchGames.id = "matchGames";
            }
            matchGames.textContent = matchState.gameScoreFirst + " - " + matchState.gameScoreSecond;
        }
    }

    function handleEvent(eventName, labelOverride) {
        eventHistory.push(eventName);
        appendLog(formatLog(eventName, labelOverride));
        sendEvent(eventName);
        applyEvent(eventName);
    }

    if (scoresAreZero()) {
        showServeQuestion(true);
        showServeButtons(true);
        setActionsDisabled(true);
        setServeButtonsDisabled(false);
        showServeStatus(true);
        showServeSideToggle(true);
        if (serveStatusText) {
            serveStatusText.textContent = "";
        }
    } else {
        showServeQuestion(false);
        showServeStatus(true);
        showServeSideToggle(false);
    }
    updateScoreDisplay();

    if (serveQuestion) {
        serveQuestion.addEventListener("click", function (event) {
            var target = event.target;
            if (!(target instanceof HTMLButtonElement)) {
                return;
            }

            if (target.dataset.serve) {
                var isLeft = serveOnLeft ? serveOnLeft.checked : false;
                setServeSide(isLeft);
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
                    matchState.lastPointWinner = target.dataset.serve;
                    matchState.currentServer = target.dataset.serve;
                    matchState.initialServeChosen = true;
                    handleEvent(serveEvent, label);
                }
                enableAfterServeChoice();
                updateServeStatus();
            }
        });
    }

    if (serveOnLeft) {
        serveOnLeft.addEventListener("change", function () {
            setServeSide(serveOnLeft.checked);
            updateServeStatus();
        });
    }

    actionButtons.forEach(function (button) {
        var eventName = button.dataset.event;
        if (!eventName) {
            return;
        }
        button.addEventListener("click", function () {
            handleEvent(eventName);
        });
    });
})();
