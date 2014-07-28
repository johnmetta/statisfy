moment.lang('ru');

var getData = function (callback,option) {

    var url = option.url;
    var expression = option.expression;
    var start = option.start;
    var stop = option.stop;

    if (!start) {
        start = new Date();
        start = new Date(start.setHours(start.getHours() - 1));
    }        

    if (!stop) {
        stop = new Date();
    }
    

    $.getJSON(url,
    {
        expression: expression,
        start: start.toISOString(),
        stop: stop.toISOString()
    },
        function (data) {
            var series = [];
            var palette = new Rickshaw.Color.Palette({ scheme: 'classic9' });
            for (var i = 0; i < data.length; i++) {

                var points = [];

                for (var j = 0; j < data[i].dataPoints.length; j++) {
                    var point = {
                        y: data[i].dataPoints[j][0],
                        x: data[i].dataPoints[j][1]
                    };
                    points[j] = point;
                }

                series[i] = {
                    data: points,
                    color: palette.color(),
                    name: data[i].target
                };
            }

            if (callback)
                callback(series);
        }
    );
};

function initChart($container) {

    var title = $container.attr('data-title');
    var expression = $container.attr('data-expression');
    var url = $container.attr('data-url');
    var interval = $container.attr('data-update-interval');

    var series = [
        {
            color: '',
            name: '',
            data: [{ x: 0, y: 0 }]
        }
    ];

    var graph;
    var legend;
    var highlighter;

    getData(function(d) {
        series.shift();

        $container.empty();

        var $chart = $('<div class="chart"></div>')
        var $chartHeader = $('<div class="chart-header"></div>')
        $chartHeader.text(title);
        var $legend = $('<div class="legend"></div>');
        $container.append($chart).append($chartHeader).append($legend).append($('<div class="clear"></div>'));

        for (var i = 0; i < d.length; i++) {
            series[i] = d[i];
        }

        graph = new Rickshaw.Graph({
            element: $chart[0],
            width: $container.width()-200,
            height: 200,
            renderer: 'area',
            stroke: true,
            preserve: true,
            series: series
        });
        
        graph.render();

        $container.fadeIn();

        var hoverDetail = new Rickshaw.Graph.HoverDetail({
            graph: graph,
            xFormatter: function(x) {
                var date = new moment(x * 1000);
                return date.format('LLLL');
            }
        });

        legend = new Rickshaw.Graph.Legend({
            graph: graph,
            element: $legend[0]

        });

        highlighter = new Rickshaw.Graph.Behavior.Series.Highlight({
            graph: graph,
            legend: legend
        });

        var ticksTreatment = 'glow';

        var xAxis = new Rickshaw.Graph.Axis.Time({
            graph: graph,
            ticksTreatment: ticksTreatment,
            timeFixture: new Rickshaw.Fixtures.Time.Local()
        });

        xAxis.render();

        var yAxis = new Rickshaw.Graph.Axis.Y({
            graph: graph,
            tickFormat: Rickshaw.Fixtures.Number.formatKMBT,
            ticksTreatment: ticksTreatment
        });

        yAxis.render();

    }, { url: url, expression: expression });    

    setInterval(function () {
       
        getData(function (d) {            

            for (var i = 0; i < d.length; i++) {
                if (series[i] && series[i].data)
                    series[i].data = d[i].data;
                else {
                    series[i] = d[i];
                }
            }

            graph.update();

        }, { url: url, expression: expression});

    }, interval);

   
}

$(document).ready(function () {
    $('.chart_container').each(function() {
        initChart($(this), 'CPU');
    });
});