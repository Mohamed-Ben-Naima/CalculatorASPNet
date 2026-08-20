(function () {
    "use strict";

    var expressionEl = document.getElementById("calcExpression");
    var resultEl = document.getElementById("calcResult");
    var errorEl = document.getElementById("calcError");
    var statusEl = document.getElementById("calcStatus");
    var historyList = document.getElementById("historyList");

    function antiForgeryToken() {
        var form = document.getElementById("antiforgeryForm");
        var input = form && form.querySelector("input[name='__RequestVerificationToken']");
        return input ? input.value : "";
    }

    function setStatus(text, state) {
        statusEl.textContent = text;
        statusEl.className = "badge rounded-pill status-badge" + (state ? " " + state : "");
    }

    function renderHistory(items) {
        historyList.innerHTML = "";
        if (!items || items.length === 0) {
            historyList.innerHTML = '<div class="text-center text-secondary p-4">No calculations yet.</div>';
            return;
        }

        items.forEach(function (item) {
            var div = document.createElement("div");
            div.className = "history-item";
            div.dataset.id = item.id;

            var expr = document.createElement("div");
            expr.className = "history-expression";
            expr.textContent = item.expression;

            var res = document.createElement("div");
            res.className = "history-result";
            res.textContent = "= " + item.result;

            div.appendChild(expr);
            div.appendChild(res);
            historyList.appendChild(div);
        });
    }

    function evaluate(expression) {
        if (!expression) {
            return;
        }

        setStatus("Calculating…", "loading");
        expressionEl.textContent = expression;

        fetch("/Home/Evaluate", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                "RequestVerificationToken": antiForgeryToken()
            },
            body: "expression=" + encodeURIComponent(expression)
        })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                if (data.success) {
                    resultEl.textContent = data.result;
                    resultEl.className = "calc-result";
                    errorEl.hidden = true;
                    setStatus("Ready");
                } else {
                    resultEl.textContent = "";
                    errorEl.textContent = data.error || "Unable to evaluate the expression.";
                    errorEl.hidden = false;
                    setStatus("Error", "error");
                }
                renderHistory(data.history);
            })
            .catch(function () {
                resultEl.textContent = "";
                errorEl.textContent = "The server is unavailable. Please try again.";
                errorEl.hidden = false;
                setStatus("Offline", "error");
            });
    }

    function clearAll() {
        expressionEl.textContent = "";
        resultEl.textContent = "";
        errorEl.hidden = true;
        setStatus("Ready");
    }

    function appendKey(key) {
        if (errorEl.hidden === false) {
            clearAll();
        }
        expressionEl.textContent += key;
    }

    function backspace() {
        var text = expressionEl.textContent;
        if (text.length > 0) {
            expressionEl.textContent = text.slice(0, -1);
        }
    }

    function handleKey(key) {
        switch (key) {
            case "clear": clearAll(); break;
            case "back": backspace(); break;
            case "=": evaluate(expressionEl.textContent); break;
            default: appendKey(key); break;
        }
    }

    document.getElementById("calcKeypad").addEventListener("click", function (e) {
        var button = e.target.closest("[data-key]");
        if (button) {
            handleKey(button.dataset.key);
        }
    });

    document.addEventListener("keydown", function (e) {
        var key = e.key;
        if (key === "Enter") {
            e.preventDefault();
            handleKey("=");
            return;
        }
        if (key === "Escape") {
            e.preventDefault();
            handleKey("clear");
            return;
        }
        if (key === "Backspace") {
            e.preventDefault();
            handleKey("back");
            return;
        }

        var accepted = /[0-9.+\-*/%^()]/.test(key);
        if (accepted && e.target.tagName !== "INPUT" && e.target.tagName !== "TEXTAREA") {
            e.preventDefault();
            var mapped = key.replace("*", "×").replace("/", "÷").replace("-", "−");
            appendKey(mapped);
        }
    });

    historyList.addEventListener("click", function (e) {
        var item = e.target.closest(".history-item");
        if (item) {
            var old = historyList.querySelector(".history-item.clicked");
            if (old) { old.classList.remove("clicked"); }
            item.classList.add("clicked");
            setTimeout(function () { item.classList.remove("clicked"); }, 250);
        }
    });

    document.getElementById("clearHistoryBtn").addEventListener("click", function () {
        fetch("/Home/DeleteHistory", {
            method: "POST",
            headers: { "RequestVerificationToken": antiForgeryToken() }
        })
            .then(function (response) { return response.json(); })
            .then(function (data) { renderHistory(data.history); })
            .catch(function () { /* ignore */ });
    });
})();