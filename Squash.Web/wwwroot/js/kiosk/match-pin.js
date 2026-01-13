(function () {
    var slots = Array.from(document.querySelectorAll(".pin-slot"));
    var keypad = document.querySelector(".keypad");
    var entryPanel = document.getElementById("panel-entry");
    var summaryPanel = document.getElementById("panel-summary");
    var pinError = document.getElementById("pinError");
    var chooseAnotherMatch = document.getElementById("chooseAnotherMatch");
    var startMatchLink = document.getElementById("startMatchLink");
    var lastPin = null;
    var storedPin = sessionStorage.getItem("matchPin");
    if (storedPin) {
        lastPin = storedPin;
    }
    if (startMatchLink && lastPin) {
        startMatchLink.setAttribute("href", "/kiosk/referee");
    }

    if (!slots.length || !keypad) {
        return;
    }

    function setActive(index) {
        slots.forEach(function (slot, i) {
            slot.classList.toggle("is-active", i === index);
        });
    }

    function getActiveIndex() {
        for (var i = 0; i < slots.length; i++) {
            if (slots[i].classList.contains("is-active")) {
                return i;
            }
        }
        return 0;
    }

    function moveToNextEmpty(start) {
        for (var i = start; i < slots.length; i++) {
            if (!slots[i].textContent) {
                setActive(i);
                return;
            }
        }
        setActive(slots.length - 1);
    }

    function resetPin() {
        slots.forEach(function (slot) {
            slot.textContent = "";
            slot.classList.remove("is-active");
        });

        if (slots.length > 0) {
            slots[0].classList.add("is-active");
        }
    }

    function updateStartMatchLabel(match) {
        if (!startMatchLink) {
            return;
        }
        var firstScore = match && typeof match.gameScoreFirst === "number" ? match.gameScoreFirst : 0;
        var secondScore = match && typeof match.gameScoreSecond === "number" ? match.gameScoreSecond : 0;
        var currentFirst = match && typeof match.currentGameScoreFirst === "number" ? match.currentGameScoreFirst : 0;
        var currentSecond = match && typeof match.currentGameScoreSecond === "number" ? match.currentGameScoreSecond : 0;
        var hasActiveGame = match && typeof match.matchGameId === "number" && match.matchGameId > 0;
        var hasEvents = match && Array.isArray(match.eventLogs) && match.eventLogs.length > 0;
        var hasStarted = hasActiveGame || hasEvents || currentFirst > 0 || currentSecond > 0 || firstScore > 0 || secondScore > 0;
        var label = hasStarted ? "Continue match" : "Start match";
        startMatchLink.textContent = label;
    }

    function isComplete() {
        return slots.every(function (slot) {
            return slot.textContent && slot.textContent.trim().length > 0;
        });
    }

    slots.forEach(function (slot, index) {
        slot.addEventListener("click", function () {
            setActive(index);
        });
    });

    function applyMatchResponse(code, data) {
        if (!data || data.success !== true) {
            if (pinError) {
                pinError.style.display = "block";
            }
            resetPin();
            return;
        }

        if (pinError) {
            pinError.style.display = "none";
        }

        if (entryPanel && summaryPanel) {
            entryPanel.classList.add("is-hidden");
            summaryPanel.classList.remove("is-hidden");
        }

        lastPin = code;
        sessionStorage.setItem("matchPin", code);
        if (data.match) {
            sessionStorage.setItem("matchData", JSON.stringify(data.match));
        }
        if (startMatchLink) {
            startMatchLink.dataset.pin = code;
            startMatchLink.setAttribute("href", "/kiosk/referee");
        }

        if (data.match) {
            var playerOneName = document.getElementById("playerOneName");
            var playerOneNation = document.getElementById("playerOneNation");
            var playerOneFlag = document.getElementById("playerOneFlag");
            var playerTwoName = document.getElementById("playerTwoName");
            var playerTwoNation = document.getElementById("playerTwoNation");
            var playerTwoFlag = document.getElementById("playerTwoFlag");
            var matchDraw = document.getElementById("matchDraw");
            var matchCourt = document.getElementById("matchCourt");

            if (playerOneName) playerOneName.textContent = data.match.firstPlayer?.name || "";
            if (playerOneNation) playerOneNation.textContent = data.match.firstPlayer?.nationality || "";
            if (playerTwoName) playerTwoName.textContent = data.match.secondPlayer?.name || "";
            if (playerTwoNation) playerTwoNation.textContent = data.match.secondPlayer?.nationality || "";
            if (playerOneFlag) {
                var flagOne = data.match.firstPlayer?.nationalityFlagUrl || "";
                playerOneFlag.src = flagOne;
                playerOneFlag.alt = data.match.firstPlayer?.nationality || "";
                playerOneFlag.style.display = flagOne ? "block" : "none";
            }
            if (playerTwoFlag) {
                var flagTwo = data.match.secondPlayer?.nationalityFlagUrl || "";
                playerTwoFlag.src = flagTwo;
                playerTwoFlag.alt = data.match.secondPlayer?.nationality || "";
                playerTwoFlag.style.display = flagTwo ? "block" : "none";
            }
            if (matchDraw) matchDraw.textContent = data.match.draw || "";
            if (matchCourt) matchCourt.textContent = data.match.court || "";
            updateStartMatchLabel(data.match);
        }
    }

    keypad.addEventListener("click", function (event) {
        var target = event.target;
        if (!(target instanceof HTMLButtonElement)) {
            return;
        }

        var value = target.textContent;
        if (!value) {
            return;
        }

        var wasComplete = isComplete();
        var activeIndex = getActiveIndex();
        slots[activeIndex].textContent = value;
        moveToNextEmpty(activeIndex + 1);

        if (!wasComplete && isComplete()) {
            var code = slots.map(function (slot) { return slot.textContent || ""; }).join("");
            fetch("/api/refferee/match?pin=" + encodeURIComponent(code))
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    applyMatchResponse(code, data);
                })
                .catch(function () {
                    if (pinError) {
                        pinError.style.display = "block";
                    }
                    resetPin();
                });
        }
    });

    if (chooseAnotherMatch) {
        chooseAnotherMatch.addEventListener("click", function () {
            resetPin();

            if (pinError) {
                pinError.style.display = "none";
            }

            if (entryPanel && summaryPanel) {
                summaryPanel.classList.add("is-hidden");
                entryPanel.classList.remove("is-hidden");
            }
            sessionStorage.removeItem("matchPin");
            sessionStorage.removeItem("matchData");
            lastPin = null;
        });
    }

    if (startMatchLink) {
        startMatchLink.addEventListener("click", function (event) {
            var pin = lastPin || startMatchLink.dataset.pin || sessionStorage.getItem("matchPin");
            if (!pin) {
                event.preventDefault();
                return;
            }

            startMatchLink.setAttribute("href", "/kiosk/referee");
        });
    }

    updateStartMatchLabel(null);

    var queryPin = new URLSearchParams(window.location.search).get("pin");
    if (queryPin && queryPin.trim().length === 6) {
        fetch("/api/refferee/match?pin=" + encodeURIComponent(queryPin.trim()))
            .then(function (response) { return response.json(); })
            .then(function (data) {
                applyMatchResponse(queryPin.trim(), data);
            })
            .catch(function () {
                if (pinError) {
                    pinError.style.display = "block";
                }
                resetPin();
            });
    }
})();
