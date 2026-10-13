"use strict";

(function () {
    const root = document.getElementById("monitoring");

    if (!root) {
        return;
    }

    const isAdmin = root.dataset.admin === "true";

    const MAX_JOURNAL_ROWS = 50;
    const MAX_ALARMS = 50;

    const elements = {
        connectionStatus: document.getElementById("connection-status"),
        simulationStatus: document.getElementById("simulation-status"),
        simulationToggle: document.getElementById("simulation-toggle"),
        error: document.getElementById("monitoring-error"),
        siteSelect: document.getElementById("site-select"),
        onlineCount: document.getElementById("online-count"),
        onlineDetails: document.getElementById("online-details"),
        sensorCards: document.getElementById("sensor-cards"),
        alarmList: document.getElementById("alarm-list"),
        alarmCount: document.getElementById("alarm-count"),
        journalBody: document.getElementById("journal-body"),
        readingForm: document.getElementById("reading-form"),
        readingSensor: document.getElementById("reading-sensor"),
        readingValue: document.getElementById("reading-value"),
        readingUnit: document.getElementById("reading-unit"),
        readingRange: document.getElementById("reading-range"),
        readingSubmit: document.getElementById("reading-submit"),
        readingResult: document.getElementById("reading-result")
    };

    const state = {
        siteId: null,
        sensors: new Map(),
        sensorCards: new Map(),
        alarms: [],
        simulationEnabled: false
    };

    const timeFormat = new Intl.DateTimeFormat("ru-RU", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit"
    });

    const dateTimeFormat = new Intl.DateTimeFormat("ru-RU", {
        day: "2-digit",
        month: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit"
    });

    const numberFormat = new Intl.NumberFormat("ru-RU", {
        maximumFractionDigits: 2
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/monitoring")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    connection.on("OnlineChanged", summary => {
        renderOnline(summary);
    });

    connection.on("ReceiveSnapshot", snapshot => {
        if ((snapshot.productionSiteId ?? null) !== state.siteId) {
            return;
        }

        state.sensors = new Map(
            snapshot.sensors.map(sensor => [sensor.id, sensor]));

        state.alarms = snapshot.activeAlarms;

        renderSensors();
        renderSensorOptions();
        renderAlarms();
        renderJournal(snapshot.recentReadings);
        renderSimulation(snapshot.simulationEnabled);
    });

    connection.on("ReceiveReading", reading => {
        if (state.siteId !== null &&
            reading.productionSiteId !== state.siteId) {
            return;
        }

        const sensor = state.sensors.get(reading.sensorId);

        if (sensor) {
            sensor.lastReading = reading;
            updateSensorCard(sensor, true);
        }

        prependJournalRow(reading);
    });

    connection.on("AlarmRaised", reading => {
        if (state.alarms.some(alarm => alarm.id === reading.id)) {
            return;
        }

        state.alarms.unshift(reading);
        state.alarms = state.alarms.slice(0, MAX_ALARMS);
        renderAlarms();
    });

    connection.on("AlarmAcknowledged", reading => {
        state.alarms = state.alarms.filter(alarm => alarm.id !== reading.id);
        renderAlarms();

        const sensor = state.sensors.get(reading.sensorId);

        if (sensor && sensor.lastReading && sensor.lastReading.id === reading.id) {
            sensor.lastReading = reading;
            updateSensorCard(sensor, false);
        }

        updateJournalRow(reading);
    });

    connection.on("SimulationStateChanged", enabled => {
        renderSimulation(enabled);
    });

    connection.on("ReadingAccepted", reading => {
        if (!elements.readingResult) {
            return;
        }

        const text = `Показание сохранено: ${reading.sensorName} — ` +
            `${formatValue(reading.value, reading.unit)}` +
            (reading.isAlarm ? ". Значение вне нормы, сформирована тревога." : ".");

        elements.readingResult.className =
            "small mt-2 " + (reading.isAlarm ? "text-danger" : "text-success");
        elements.readingResult.textContent = text;
    });

    connection.onreconnecting(() => {
        setConnectionStatus("reconnecting");
    });

    connection.onreconnected(async () => {
        setConnectionStatus("connected");
        await joinSite(state.siteId);
    });

    connection.onclose(() => {
        setConnectionStatus("disconnected");
        scheduleStart();
    });

    async function joinSite(siteId) {
        try {
            await connection.invoke("JoinSite", siteId);
        } catch (error) {
            showError(error);
        }
    }

    async function submitReading(sensorId, value) {
        await connection.invoke("SubmitReading", sensorId, value);
    }

    async function acknowledgeAlarm(readingId) {
        await connection.invoke("AcknowledgeAlarm", readingId);
    }

    async function setSimulationEnabled(enabled) {
        await connection.invoke("SetSimulationEnabled", enabled);
    }

    elements.siteSelect.addEventListener("change", async () => {
        state.siteId = parseSiteId(elements.siteSelect.value);

        if (connection.state === signalR.HubConnectionState.Connected) {
            await joinSite(state.siteId);
        }
    });

    if (elements.readingForm) {
        elements.readingSensor.addEventListener("change", renderSelectedSensorHint);

        elements.readingForm.addEventListener("submit", async event => {
            event.preventDefault();

            const sensorId = Number(elements.readingSensor.value);
            const rawValue = elements.readingValue.value.trim();
            const value = Number(rawValue);

            if (!sensorId) {
                showReadingError("Выберите датчик.");
                return;
            }

            if (rawValue === "" || !Number.isFinite(value)) {
                showReadingError("Введите числовое значение.");
                return;
            }

            elements.readingSubmit.disabled = true;
            elements.readingResult.textContent = "";

            try {
                await submitReading(sensorId, value);
                elements.readingValue.value = "";
            } catch (error) {
                showReadingError(errorText(error));
            } finally {
                elements.readingSubmit.disabled = false;
            }
        });
    }

    if (isAdmin) {
        elements.alarmList.addEventListener("click", async event => {
            const button = event.target.closest("button[data-reading-id]");

            if (!button) {
                return;
            }

            button.disabled = true;

            try {
                await acknowledgeAlarm(Number(button.dataset.readingId));
            } catch (error) {
                button.disabled = false;
                showError(error);
            }
        });
    }

    if (elements.simulationToggle) {
        elements.simulationToggle.addEventListener("click", async () => {
            elements.simulationToggle.disabled = true;

            try {
                await setSimulationEnabled(!state.simulationEnabled);
            } catch (error) {
                showError(error);
            } finally {
                elements.simulationToggle.disabled = false;
            }
        });
    }

    function renderOnline(summary) {
        elements.onlineCount.textContent = summary.totalCount;

        const parts = [];

        if (summary.userNames.length > 0) {
            parts.push("пользователи: " + summary.userNames.join(", "));
        }

        if (summary.guestCount > 0) {
            parts.push("гостей: " + summary.guestCount);
        }

        elements.onlineDetails.textContent = parts.join("; ");
    }

    function renderSimulation(enabled) {
        state.simulationEnabled = enabled;

        elements.simulationStatus.className =
            "badge " + (enabled ? "bg-success" : "bg-secondary");
        elements.simulationStatus.textContent =
            enabled ? "Имитатор: работает" : "Имитатор: остановлен";

        if (elements.simulationToggle) {
            elements.simulationToggle.textContent =
                enabled ? "Остановить имитатор" : "Запустить имитатор";
            elements.simulationToggle.disabled =
                connection.state !== signalR.HubConnectionState.Connected;
        }
    }

    function renderSensors() {
        elements.sensorCards.replaceChildren();
        state.sensorCards.clear();

        if (state.sensors.size === 0) {
            elements.sensorCards.append(
                createElement("div", "col text-muted", "Датчики не найдены."));
            return;
        }

        for (const sensor of state.sensors.values()) {
            const card = createSensorCard();
            state.sensorCards.set(sensor.id, card);
            elements.sensorCards.append(card.column);
            updateSensorCard(sensor, false);
        }
    }

    function createSensorCard() {
        const column = createElement("div", "col");
        const card = createElement("div", "card h-100 sensor-card");
        const body = createElement("div", "card-body");

        const location = createElement("div", "small text-muted");
        const name = createElement("div", "fw-semibold mb-1");
        const value = createElement("div", "sensor-value");
        const range = createElement("div", "small text-muted");
        const updated = createElement("div", "small text-muted");

        body.append(location, name, value, range, updated);
        card.append(body);
        column.append(card);

        return { column, card, location, name, value, range, updated };
    }

    function updateSensorCard(sensor, highlight) {
        const card = state.sensorCards.get(sensor.id);

        if (!card) {
            return;
        }

        const reading = sensor.lastReading;
        const status = readingStatus(reading);

        card.card.className = "card h-100 sensor-card border-" + status.color;
        card.location.textContent =
            `${sensor.productionSiteName} · ${sensor.equipmentName}`;
        card.name.textContent = sensor.name;
        card.value.className = "sensor-value text-" + status.color;
        card.value.textContent = reading
            ? formatValue(reading.value, sensor.unit)
            : "нет данных";
        card.range.textContent =
            "Норма: " + formatRange(sensor.minValue, sensor.maxValue, sensor.unit);
        card.updated.textContent = reading
            ? `${status.text} · ${timeFormat.format(new Date(reading.timestamp))}`
            : "";

        if (highlight) {
            card.card.classList.remove("sensor-updated");
            void card.card.offsetWidth;
            card.card.classList.add("sensor-updated");
        }
    }

    function renderSensorOptions() {
        if (!elements.readingSensor) {
            return;
        }

        const selectedId = elements.readingSensor.value;
        elements.readingSensor.replaceChildren();

        for (const sensor of state.sensors.values()) {
            const option = createElement(
                "option",
                null,
                `${sensor.name} (${sensor.equipmentName})`);

            option.value = sensor.id;
            elements.readingSensor.append(option);
        }

        if (state.sensors.has(Number(selectedId))) {
            elements.readingSensor.value = selectedId;
        }

        renderSelectedSensorHint();
    }

    function renderSelectedSensorHint() {
        const sensor = state.sensors.get(Number(elements.readingSensor.value));

        elements.readingUnit.textContent = sensor ? sensor.unit : "";
        elements.readingRange.textContent = sensor
            ? "Норма: " + formatRange(sensor.minValue, sensor.maxValue, sensor.unit)
            : "";
    }

    function renderAlarms() {
        elements.alarmList.replaceChildren();
        elements.alarmCount.textContent = state.alarms.length;

        if (state.alarms.length === 0) {
            elements.alarmList.append(createElement(
                "li",
                "list-group-item text-muted",
                "Активных тревог нет."));
            return;
        }

        for (const alarm of state.alarms) {
            const item = createElement(
                "li",
                "list-group-item list-group-item-danger d-flex justify-content-between align-items-start gap-2");

            const info = createElement("div");

            info.append(
                createElement(
                    "div",
                    "fw-semibold",
                    `${alarm.sensorName}: ${formatValue(alarm.value, alarm.unit)}`),
                createElement(
                    "div",
                    "small",
                    `${alarm.productionSiteName} · ${alarm.equipmentName} · норма ` +
                    formatRange(alarm.minValue, alarm.maxValue, alarm.unit)),
                createElement(
                    "div",
                    "small text-muted",
                    `${dateTimeFormat.format(new Date(alarm.timestamp))} · источник: ${alarm.source}`));

            item.append(info);

            if (isAdmin) {
                const button = createElement(
                    "button",
                    "btn btn-sm btn-outline-danger flex-shrink-0",
                    "Квитировать");

                button.type = "button";
                button.dataset.readingId = alarm.id;
                item.append(button);
            }

            elements.alarmList.append(item);
        }
    }

    function renderJournal(readings) {
        elements.journalBody.replaceChildren();

        for (const reading of readings) {
            elements.journalBody.append(createJournalRow(reading));
        }
    }

    function prependJournalRow(reading) {
        elements.journalBody.prepend(createJournalRow(reading));

        while (elements.journalBody.rows.length > MAX_JOURNAL_ROWS) {
            elements.journalBody.lastElementChild.remove();
        }
    }

    function updateJournalRow(reading) {
        const row = elements.journalBody.querySelector(
            `tr[data-reading-id="${reading.id}"]`);

        if (row) {
            row.replaceWith(createJournalRow(reading));
        }
    }

    function createJournalRow(reading) {
        const status = readingStatus(reading);
        const row = createElement("tr", status.rowClass);
        row.dataset.readingId = reading.id;

        const value = createElement(
            "td",
            "text-end text-nowrap",
            formatValue(reading.value, reading.unit));

        const statusCell = createElement("td");
        statusCell.append(createElement(
            "span",
            "badge bg-" + status.color,
            status.text));

        if (reading.isAcknowledged && reading.acknowledgedBy) {
            statusCell.title = `Квитировал: ${reading.acknowledgedBy}`;
        }

        const sensorCell = createElement("td");
        sensorCell.append(
            createElement("div", null, reading.sensorName),
            createElement(
                "div",
                "small text-muted",
                `${reading.productionSiteName} · ${reading.equipmentName}`));

        row.append(
            createElement(
                "td",
                "text-nowrap",
                timeFormat.format(new Date(reading.timestamp))),
            sensorCell,
            value,
            createElement("td", "small", reading.source),
            statusCell);

        return row;
    }

    function readingStatus(reading) {
        if (!reading) {
            return { color: "secondary", text: "нет данных", rowClass: "" };
        }

        if (reading.isAlarm && !reading.isAcknowledged) {
            return { color: "danger", text: "тревога", rowClass: "table-danger" };
        }

        if (reading.isAlarm) {
            return { color: "warning", text: "квитировано", rowClass: "table-warning" };
        }

        return { color: "success", text: "норма", rowClass: "" };
    }

    function setConnectionStatus(status) {
        const statuses = {
            connecting: ["bg-secondary", "Подключение…"],
            connected: ["bg-success", "Подключено"],
            reconnecting: ["bg-warning text-dark", "Переподключение…"],
            disconnected: ["bg-danger", "Нет связи с сервером"]
        };

        const [className, text] = statuses[status];
        elements.connectionStatus.className = "badge " + className;
        elements.connectionStatus.textContent = text;

        if (elements.simulationToggle) {
            elements.simulationToggle.disabled = status !== "connected";
        }

        if (elements.readingSubmit) {
            elements.readingSubmit.disabled = status !== "connected";
        }

        if (status === "connected") {
            hideError();
        }
    }

    function parseSiteId(value) {
        return value === "" ? null : Number(value);
    }

    function formatValue(value, unit) {
        return `${numberFormat.format(value)} ${unit}`;
    }

    function formatRange(min, max, unit) {
        return `${numberFormat.format(min)} – ${numberFormat.format(max)} ${unit}`;
    }

    function errorText(error) {
        const message = error && error.message ? error.message : String(error);
        const marker = "HubException: ";
        const index = message.lastIndexOf(marker);

        if (index >= 0) {
            return message.substring(index + marker.length);
        }

        if (message.includes("unauthorized")) {
            return "Недостаточно прав для выполнения операции.";
        }

        return message;
    }

    function showError(error) {
        elements.error.textContent = errorText(error);
        elements.error.classList.remove("d-none");
    }

    function hideError() {
        elements.error.classList.add("d-none");
        elements.error.textContent = "";
    }

    function showReadingError(text) {
        elements.readingResult.className = "small mt-2 text-danger";
        elements.readingResult.textContent = text;
    }

    function createElement(tagName, className, text) {
        const element = document.createElement(tagName);

        if (className) {
            element.className = className;
        }

        if (text !== undefined) {
            element.textContent = text;
        }

        return element;
    }

    async function start() {
        if (connection.state !== signalR.HubConnectionState.Disconnected) {
            return;
        }

        setConnectionStatus("connecting");

        try {
            await connection.start();
            setConnectionStatus("connected");
            await joinSite(state.siteId);
        } catch (error) {
            setConnectionStatus("disconnected");
            scheduleStart();
        }
    }

    function scheduleStart() {
        setTimeout(start, 5000);
    }

    state.siteId = parseSiteId(elements.siteSelect.value);

    start();
})();
