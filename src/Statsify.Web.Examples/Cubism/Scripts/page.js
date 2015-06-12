var metrics = [
    'servers.srv-aps11.system.processor.total_time',
    'servers.mow1aps1.system.processor.total_time'
];

var context = cubism.context()
    .step(10 * 1000 * 5)
    .size(1440);

d3.select("body").selectAll(".axis")
    .data(["top", "bottom"])
  .enter().append("div")
    .attr("class", function (d) { return d + " axis"; })
    .each(function (d) { d3.select(this).call(context.axis().ticks(12).orient(d)); });

d3.select("body").append("div")
    .attr("class", "rule")
    .call(context.rule());

d3.select("body").selectAll(".horizon")
    .data(metrics.map(getData))
  .enter().insert("div", ".bottom")
    .attr("class", "horizon")
    .call(context.horizon().extent([-100, 100]));

context.on("focus", function (i) {
    d3.selectAll(".value").style("right", i == null ? null : context.size() - i + "px");
});

function getData(expression) {

    var value = 0,
        values = [],
        i = 0,
        last;

    return context.metric(function (start, stop, step, callback) {

        $.getJSON('http://mow1aps3/statsify/api/v1/series', {
            from: start.toISOString(),
            until: stop.toISOString(),
            expression: expression
        }, function (data) {

            debugger;

            start = +start, stop = +stop;

            if (isNaN(last)) last = start;

            var index = 0;

            while (last < stop) {
                last += step;
                var datapoint = data[0].datapoints[index];
                if(!datapoint) break;

                value = datapoint[0];
                values.push(value);
                index++;
            }

            values = values.slice((start - stop) / step);

            callback(null, values);
        });

       
    }, expression);
}
