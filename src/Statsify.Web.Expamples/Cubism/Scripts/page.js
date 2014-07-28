var metrics = [
    'servers.n46-msk.system.processor.total_privileged_time',
    'servers.n46-msk.system.processor.total_time',
    'servers.n46-msk.system.processor.total_user_time',


    'servers.n46-msk.system.physical_disk.average_queue_length',
    'servers.n46-msk.system.physical_disk.average_sec_read',
    'servers.n46-msk.system.physical_disk.average_sec_write',
    'servers.n46-msk.system.physical_disk.bytes_sec',
    'servers.n46-msk.system.physical_disk.current_queue_length',
    'servers.n46-msk.system.physical_disk.disk_time',
    'servers.n46-msk.system.physical_disk.read_bytes_sec',
    'servers.n46-msk.system.physical_disk.transfers_sec',
    'servers.n46-msk.system.physical_disk.write_bytes_sec',

    'servers.n46-msk.system.memory.available_mb',

    'servers.n46-msk.system.asp_net.requests_sec'
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
    .call(context.horizon().extent([-10, 10]));

context.on("focus", function (i) {
    d3.selectAll(".value").style("right", i == null ? null : context.size() - i + "px");
});

function getData(expression) {

    var value = 0,
        values = [],
        i = 0,
        last;

    return context.metric(function (start, stop, step, callback) {

        $.getJSON('http://localhost/Statsify/api/series', {
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
