window.stockTickerDialog = (() => {
    const charts = new Map();

    function open(dialog) {
        if (!dialog || dialog.open) {
            return;
        }

        dialog.showModal();
    }

    function close(dialog) {
        if (!dialog || !dialog.open) {
            return;
        }

        dialog.close();
    }

    function destroyChart(canvasId) {
        const existing = charts.get(canvasId);
        if (!existing) {
            return;
        }

        existing.destroy();
        charts.delete(canvasId);
    }

    function renderPriceChart(canvasId, ticker, labels, values) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        destroyChart(canvasId);

        if (!window.Chart) {
            throw new Error("Chart.js is not loaded.");
        }

        const currency = new Intl.NumberFormat(undefined, {
            style: "currency",
            currency: "USD",
            maximumFractionDigits: 2
        });

        const chart = new Chart(canvas, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label: `${ticker} close price`,
                    data: values,
                    borderColor: "#0d5c63",
                    backgroundColor: "rgba(13, 92, 99, 0.12)",
                    borderWidth: 2.5,
                    pointRadius: 0,
                    pointHoverRadius: 4,
                    tension: 0.2,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    intersect: false,
                    mode: "index"
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: context => currency.format(context.parsed.y)
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            maxTicksLimit: 8
                        }
                    },
                    y: {
                        ticks: {
                            callback: value => currency.format(value)
                        }
                    }
                }
            }
        });

        charts.set(canvasId, chart);
    }

    return {
        open,
        close,
        destroyChart,
        renderPriceChart
    };
})();
