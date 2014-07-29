var metrics = [
    'servers.mow1aps3.system.processor.total_time',
    'servers.srv-aps11.system.processor.total_time',
    'servers.srv-aps29.system.processor.total_time'
];

var context = cubism.context()
    .step(1e4)
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
    .call(context.horizon().extent([0, 20]));

context.on("focus", function (i) {
    d3.selectAll(".value").style("right", i == null ? null : context.size() - i + "px");
});

function getData(expression) {

    var value = 0,
        values = [],
        i = 0,
        last;

    return context.metric(function (start, stop, step, callback) {

        $.getJSON('http://mow1aps3:8081/Statsify/api/series', {
            start: start.toISOString(),
            stop: stop.toISOString(),
            expression: 'List("' + expression + '")'
        }, function (data) {

            start = +start, stop = +stop;

            if (isNaN(last)) last = start;

            var index = 0;

            while (last < stop) {
                last += step;
                value = data[0].dataPoints[index][0];
                values.push(value);
                index++;
            }

            values = values.slice((start - stop) / step);

            callback(null, values);
        });

       
    }, expression);
}
