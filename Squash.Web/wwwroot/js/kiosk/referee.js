(function () {
    var root = document.querySelector(".screen");
    if (!root) {
        return;
    }

    var serveQuestion = document.getElementById("serveQuestion");
    var actionButtons = Array.from(document.querySelectorAll(".referee-action"));
    var actionPanels = Array.from(document.querySelectorAll(".button-grid, .log-card"));
    var serveButtons = Array.from(document.querySelectorAll(".serve-choice"));
    var eventLog = document.getElementById("eventLog");
    var scoreLog = document.getElementById("scoreLog");
    var serveStatus = document.getElementById("serveStatus");
    var serveStatusText = document.getElementById("serveStatusText");
    var nextGameButton = document.getElementById("nextGameButton");
    var serveSideToggle = document.getElementById("serveSideToggle");
    var serveOnLeft = document.getElementById("serveOnLeft");
    var refereePanel = document.getElementById("refereePanel");
    var morePanel = document.getElementById("morePanel");
    var showMoreButton = document.getElementById("showMoreButton");
    var backToMatchButton = document.getElementById("backToMatch");
    var serveNames = {
        A: serveButtons[0] ? serveButtons[0].textContent.replace(" serves first", "").trim() : "Player A",
        B: serveButtons[1] ? serveButtons[1].textContent.replace(" serves first", "").trim() : "Player B"
    };

    var matchState = {
        gameScoreFirst: parseByte(root.dataset.gameScoreFirst),
        gameScoreSecond: parseByte(root.dataset.gameScoreSecond),
        currentGameScoreFirst: parseByte(root.dataset.currentGameScoreFirst),
        currentGameScoreSecond: parseByte(root.dataset.currentGameScoreSecond),
        gamesToWin: parseByte(root.dataset.gamesToWin),
        lastPointWinner: null,
        currentServer: null,
        serveSide: "right",
        initialServeChosen: false,
        awaitingSideChoice: false
    };

    var eventHistory = [];
    var storageKey = "refereeState:" + (sessionStorage.getItem("matchPin") || "default");
    var activePanel = "referee";

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

    function showActionPanels(show) {
        actionPanels.forEach(function (panel) {
            panel.style.display = show ? "" : "none";
        });
    }

    function isVisible(element) {
        if (!element) {
            return false;
        }
        return window.getComputedStyle(element).display !== "none";
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
        ensureServeTogglePlacement();
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
        ensureServeTogglePlacement();
    }

    function showServeSideToggle(show) {
        if (!serveSideToggle) {
            return;
        }
        serveSideToggle.style.display = show ? "inline-flex" : "none";
        ensureServeTogglePlacement();
    }

    function ensureServeTogglePlacement() {
        if (!serveSideToggle || !serveQuestion || !serveStatus) {
            return;
        }

        var serveQuestionVisible = serveQuestion.style.display !== "none";
        var target = serveQuestionVisible ? serveQuestion : serveStatus;
        if (serveSideToggle.parentElement !== target) {
            target.appendChild(serveSideToggle);
        }
    }

    function enableAfterServeChoice() {
        showServeQuestion(false);
        setActionsDisabled(false);
        setServeButtonsDisabled(false);
        showServeStatus(true);
        showActionPanels(true);
        showServeSideToggle(false);
        matchState.awaitingSideChoice = false;
        hideNextGameButton();
        saveState();
    }

    function setServeSide(isLeft) {
        matchState.serveSide = isLeft ? "left" : "right";
        if (serveOnLeft) {
            serveOnLeft.checked = isLeft;
        }
    }

    function showPanel(name) {
        activePanel = name;
        if (refereePanel) {
            refereePanel.classList.toggle("is-hidden", name !== "referee");
        }
        if (morePanel) {
            morePanel.classList.toggle("is-hidden", name !== "more");
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

    function appendScoreLogEntry(side, score, sideLetter, allowSideUpdate) {
        if (!scoreLog) {
            return;
        }

        var row = document.createElement("div");
        row.className = "score-log__row";
        row.dataset.type = "entry";
        row.dataset.side = side;
        row.dataset.score = String(score);
        row.dataset.sideChoice = allowSideUpdate ? "true" : "false";

        var leftCell = document.createElement("div");
        leftCell.className = "score-log__cell score-log__cell--left";

        var rightCell = document.createElement("div");
        rightCell.className = "score-log__cell score-log__cell--right";

        var text = String(score) + sideLetter;
        if (side === "A") {
            leftCell.textContent = text;
        } else {
            rightCell.textContent = text;
        }

        row.appendChild(leftCell);
        row.appendChild(rightCell);
        scoreLog.appendChild(row);
        scrollScoreLogToBottom();
    }

    function appendScoreLogFull(text) {
        if (!scoreLog) {
            return;
        }

        var row = document.createElement("div");
        row.className = "score-log__row score-log__row--full";
        row.dataset.type = "full";

        var cell = document.createElement("div");
        cell.className = "score-log__cell score-log__cell--full";
        cell.textContent = text;

        row.appendChild(cell);
        scoreLog.appendChild(row);
        scrollScoreLogToBottom();
    }

    function scrollScoreLogToBottom() {
        if (!scoreLog) {
            return;
        }
        scoreLog.scrollTop = scoreLog.scrollHeight;
    }

    function updateLastScoreLogSide(sideLetter) {
        if (!scoreLog || !scoreLog.lastElementChild) {
            return;
        }

        var row = scoreLog.lastElementChild;
        if (row.dataset.type !== "entry" || row.dataset.sideChoice !== "true") {
            return;
        }

        var score = row.dataset.score || "0";
        var text = score + sideLetter;
        if (row.dataset.side === "A") {
            var leftCell = row.querySelector(".score-log__cell--left");
            if (leftCell) {
                leftCell.textContent = text;
            }
        } else {
            var rightCell = row.querySelector(".score-log__cell--right");
            if (rightCell) {
                rightCell.textContent = text;
            }
        }
    }

    function clearLog() {
        if (!eventLog) {
            return;
        }
        eventLog.innerHTML = "";
    }

    function clearScoreLog() {
        if (!scoreLog) {
            return;
        }
        scoreLog.innerHTML = "";
    }

    function saveState() {
        if (!storageKey) {
            return;
        }
        var logEntries = eventLog ? Array.from(eventLog.children).map(function (entry) {
            return entry.textContent || "";
        }) : [];
        var scoreEntries = scoreLog ? Array.from(scoreLog.children).map(function (row) {
            if (row.dataset.type === "full") {
                return {
                    type: "full",
                    text: row.textContent || ""
                };
            }

            return {
                type: "entry",
                side: row.dataset.side || "",
                score: row.dataset.score || "",
                sideChoice: row.dataset.sideChoice || "false",
                left: row.querySelector(".score-log__cell--left")?.textContent || "",
                right: row.querySelector(".score-log__cell--right")?.textContent || ""
            };
        }) : [];

        var state = {
            matchState: matchState,
            activePanel: activePanel,
            ui: {
                serveQuestionVisible: isVisible(serveQuestion),
                actionPanelsVisible: actionPanels.length > 0 ? isVisible(actionPanels[0]) : false,
                serveStatusVisible: isVisible(serveStatus),
                serveSideToggleVisible: isVisible(serveSideToggle),
                nextGameAction: nextGameButton ? nextGameButton.dataset.action || "" : "",
                nextGameText: nextGameButton ? nextGameButton.textContent || "" : "",
                serveStatusText: serveStatusText ? serveStatusText.textContent || "" : ""
            },
            log: logEntries,
            scoreLog: scoreEntries
        };

        sessionStorage.setItem(storageKey, JSON.stringify(state));
    }

    function restoreState() {
        if (!storageKey) {
            return false;
        }
        var raw = sessionStorage.getItem(storageKey);
        if (!raw) {
            return false;
        }

        var saved;
        try {
            saved = JSON.parse(raw);
        } catch (error) {
            return false;
        }

        if (!saved || !saved.matchState) {
            return false;
        }

        Object.keys(saved.matchState).forEach(function (key) {
            if (Object.prototype.hasOwnProperty.call(matchState, key)) {
                matchState[key] = saved.matchState[key];
            }
        });

        showPanel(saved.activePanel === "more" ? "more" : "referee");

        if (serveOnLeft) {
            serveOnLeft.checked = matchState.serveSide === "left";
        }

        if (saved.ui) {
            showServeQuestion(!!saved.ui.serveQuestionVisible);
            showActionPanels(!!saved.ui.actionPanelsVisible);
            showServeStatus(!!saved.ui.serveStatusVisible);
            showServeSideToggle(!!saved.ui.serveSideToggleVisible);

            if (nextGameButton) {
                if (saved.ui.nextGameAction) {
                    nextGameButton.dataset.action = saved.ui.nextGameAction;
                    nextGameButton.textContent = saved.ui.nextGameText || nextGameButton.textContent;
                    nextGameButton.style.display = "inline-flex";
                } else {
                    hideNextGameButton();
                }
            }

            if (serveStatusText && saved.ui.serveStatusText) {
                serveStatusText.textContent = saved.ui.serveStatusText;
            }
        }

        if (Array.isArray(saved.log)) {
            clearLog();
            for (var i = saved.log.length - 1; i >= 0; i -= 1) {
                appendLog(saved.log[i]);
            }
        }

        if (scoreLog && Array.isArray(saved.scoreLog)) {
            clearScoreLog();
            saved.scoreLog.forEach(function (entry) {
                if (!entry) {
                    return;
                }
                if (entry.type === "full") {
                    appendScoreLogFull(entry.text || "");
                    return;
                }
                if (!entry.side) {
                    return;
                }
                var row = document.createElement("div");
                row.className = "score-log__row";
                row.dataset.type = "entry";
                row.dataset.side = entry.side;
                row.dataset.score = entry.score || "0";
                row.dataset.sideChoice = entry.sideChoice || "false";

                var leftCell = document.createElement("div");
                leftCell.className = "score-log__cell score-log__cell--left";
                leftCell.textContent = entry.left || "";

                var rightCell = document.createElement("div");
                rightCell.className = "score-log__cell score-log__cell--right";
                rightCell.textContent = entry.right || "";

                row.appendChild(leftCell);
                row.appendChild(rightCell);
                scoreLog.appendChild(row);
            });
        }

        updateScoreDisplay();
        if (!nextGameButton || !nextGameButton.dataset.action) {
            updateServeStatus();
        }

        return true;
    }

    function sendEvent(eventName) {
        fetch("/api/refferee/game-log", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(eventName)
        });
    }

    function formatLog(eventName, labelOverride, scoreSnapshot) {
        if (labelOverride) {
            return labelOverride;
        }

        function nameFor(side) {
            return serveNames[side] || side;
        }

        function scoreText(snapshot) {
            if (snapshot) {
                return snapshot.a + ":" + snapshot.b;
            }
            return matchState.currentGameScoreFirst + ":" + matchState.currentGameScoreSecond;
        }

        function pointLabel(prefix, side) {
            return prefix + " " + nameFor(side) + " (" + scoreText(scoreSnapshot) + ")";
        }

        switch (eventName) {
            case "PointA":
                return pointLabel("Point to", "A");
            case "PointB":
                return pointLabel("Point to", "B");
            case "StrokeA":
                return pointLabel("Stroke to", "A");
            case "StrokeB":
                return pointLabel("Stroke to", "B");
            case "ConductStrokeA":
                return pointLabel("Conduct stroke to", "A");
            case "ConductStrokeB":
                return pointLabel("Conduct stroke to", "B");
            case "WarningA":
                return "Warning to " + nameFor("A");
            case "WarningB":
                return "Warning to " + nameFor("B");
            case "ARequestReview":
                return nameFor("A") + " requests review";
            case "BRequestReview":
                return nameFor("B") + " requests review";
            case "ARetires":
                return nameFor("A") + " retires";
            case "BRetires":
                return nameFor("B") + " retires";
            case "InjuryTimeoutA":
                return "Injury timeout for " + nameFor("A");
            case "InjuryTimeoutB":
                return "Injury timeout for " + nameFor("B");
            case "EquipmentIssue":
                return "Equipment issue";
            case "Let":
                return "Yes, let";
            case "Undo":
                return "Undo";
            case "LockMatch":
                return "Lock match";
            default:
                return eventName;
        }
    }

    function applyEvent(eventName) {
        var needsPointA = eventName === "PointA" || eventName === "StrokeA" || eventName === "ConductStrokeA";
        var needsPointB = eventName === "PointB" || eventName === "StrokeB" || eventName === "ConductStrokeB";
        var scoreSnapshot = null;
        var scoreLogSnapshot = null;

        if (needsPointA) {
            matchState.currentGameScoreFirst += 1;
        }
        if (needsPointB) {
            matchState.currentGameScoreSecond += 1;
        }

        if (needsPointA || needsPointB) {
            scoreSnapshot = {
                a: matchState.currentGameScoreFirst,
                b: matchState.currentGameScoreSecond
            };
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

            scoreLogSnapshot = {
                side: currentWinner,
                score: currentWinner === "A"
                    ? matchState.currentGameScoreFirst
                    : matchState.currentGameScoreSecond,
                sideLetter: matchState.serveSide === "left" ? "L" : "R",
                allowSideUpdate: isHandout
            };
            updateServeStatus();
            updateScoreDisplay();

            if (isGameOver()) {
                var winner = matchState.currentGameScoreFirst > matchState.currentGameScoreSecond ? "A" : "B";
                applyEndOfGame(winner);
            }
        }

        if (eventName === "ARetires") {
            if (!confirm("A retires. B wins the match. Continue?")) {
                return { cancelled: true };
            }
            window.location.href = "/m";
            return { scoreSnapshot: scoreSnapshot, scoreLogSnapshot: scoreLogSnapshot };
        }

        if (eventName === "BRetires") {
            if (!confirm("B retires. A wins the match. Continue?")) {
                return { cancelled: true };
            }
            window.location.href = "/m";
            return { scoreSnapshot: scoreSnapshot, scoreLogSnapshot: scoreLogSnapshot };
        }

        if (eventName === "InjuryTimeoutA" || eventName === "InjuryTimeoutB") {
            return;
        }

        if (eventName === "EquipmentIssue") {
            return;
        }

        return { scoreSnapshot: scoreSnapshot, scoreLogSnapshot: scoreLogSnapshot };
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
        showActionPanels(false);
        showServeQuestion(false);
        updateServeStatus();
        updateScoreDisplay();

        logDerivedEvent("EndGame", "Game to " + (serveNames[winner] || winner));
        appendScoreLogFull("---------");
        appendScoreLogFull(matchState.gameScoreFirst + " : " + matchState.gameScoreSecond);

        var gamesToWin = matchState.gamesToWin || 3;
        if (matchState.gameScoreFirst >= gamesToWin || matchState.gameScoreSecond >= gamesToWin) {
            logDerivedEvent("EndMatch", "Match won by " + (serveNames[winner] || winner));
            showFinishButton(winner);
        } else {
            showNextGameButton();
        }
        saveState();
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
        var result = applyEvent(eventName);
        if (result && result.cancelled) {
            return;
        }
        appendLog(formatLog(eventName, labelOverride, result ? result.scoreSnapshot : null));
        if (result && result.scoreLogSnapshot) {
            appendScoreLogEntry(
                result.scoreLogSnapshot.side,
                result.scoreLogSnapshot.score,
                result.scoreLogSnapshot.sideLetter,
                result.scoreLogSnapshot.allowSideUpdate
            );
        }
        sendEvent(eventName);
        saveState();
    }

    var restored = restoreState();
    if (!restored) {
        showPanel("referee");
        if (scoresAreZero()) {
            showServeQuestion(true);
            showServeButtons(true);
            setActionsDisabled(true);
            setServeButtonsDisabled(false);
            showServeStatus(false);
            showActionPanels(false);
            showServeSideToggle(true);
            hideNextGameButton();
            if (serveStatusText) {
                serveStatusText.textContent = "";
            }
        } else {
            showServeQuestion(false);
            showServeStatus(true);
            showActionPanels(true);
            showServeSideToggle(false);
            hideNextGameButton();
        }
        updateScoreDisplay();
        saveState();
    }

    function showNextGameButton() {
        if (!nextGameButton || !serveStatusText) {
            return;
        }
        var nextGameNumber = matchState.gameScoreFirst + matchState.gameScoreSecond + 1;
        nextGameButton.textContent = "Start game " + nextGameNumber;
        nextGameButton.dataset.action = "next-game";
        nextGameButton.style.display = "inline-flex";
        serveStatusText.textContent = "Game to " + (serveNames[matchState.currentServer] || matchState.currentServer);
    }

    function showFinishButton(winner) {
        if (!nextGameButton || !serveStatusText) {
            return;
        }
        nextGameButton.textContent = "Finish";
        nextGameButton.dataset.action = "finish-match";
        nextGameButton.style.display = "inline-flex";
        serveStatusText.textContent = "Match to " + (serveNames[winner] || winner);
    }

    function hideNextGameButton() {
        if (!nextGameButton) {
            return;
        }
        nextGameButton.style.display = "none";
        delete nextGameButton.dataset.action;
    }

    function startNextGame() {
        showActionPanels(true);
        showServeStatus(true);
        showServeQuestion(false);
        showServeSideToggle(true);
        matchState.awaitingSideChoice = false;
        setServeSide(false);
        clearLog();
        clearScoreLog();
        appendScoreLogEntry(matchState.currentServer, 0, "R", true);
        updateServeStatus();
        saveState();
    }

    if (nextGameButton) {
        nextGameButton.addEventListener("click", function () {
            if (nextGameButton.dataset.action === "finish-match") {
                window.location.href = "/m";
                return;
            }
            hideNextGameButton();
            startNextGame();
        });
    }

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
                    clearScoreLog();
                    appendScoreLogEntry(
                        target.dataset.serve,
                        0,
                        isLeft ? "L" : "R",
                        true
                    );
                }
                enableAfterServeChoice();
                updateServeStatus();
                saveState();
            }
        });
    }

    if (serveOnLeft) {
        serveOnLeft.addEventListener("change", function () {
            setServeSide(serveOnLeft.checked);
            updateServeStatus();
            updateLastScoreLogSide(serveOnLeft.checked ? "L" : "R");
            saveState();
        });
    }

    if (showMoreButton) {
        showMoreButton.addEventListener("click", function () {
            showPanel("more");
            saveState();
        });
    }

    if (backToMatchButton) {
        backToMatchButton.addEventListener("click", function () {
            showPanel("referee");
            saveState();
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
