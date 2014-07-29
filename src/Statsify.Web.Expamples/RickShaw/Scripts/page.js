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
            var palette = new Rickshaw.Color.Palette({ scheme: 'spectrum14' });
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
    var renderer = $container.attr('data-renderer');

    if (!renderer)
        renderer = 'area';

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
        if(d.length)
			series.shift();
			
        $container.empty();
        $container.append('<div class="chart-header"></div><table width="100%"><tr><td class="control">' +            
            '</td><td><div class="chart"></div></td><td><div class="legend"></div></td></tr></table>');
        var $chart = $container.find('.chart');
        var $chartHeader = $container.find('.chart-header');
        $chartHeader.text(title);
        var $legend = $container.find('.legend');
        $container.find('.control').append($('.panel_template').clone().children());

       
        for (var i = 0; i < d.length; i++) {
            series[i] = d[i];			
        }
        var width = $('body').width()-80;
        $container.css({ width: width + 'px' });
        
        graph = new Rickshaw.Graph({
            element: $chart[0],
            width: width - 500,
            height: 250,
            renderer: renderer,
            stroke: true,
            preserve: true,
            series: series
        });
        
        graph.render();

		if(d.length)
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

        var shelving = new Rickshaw.Graph.Behavior.Series.Toggle({
            graph: graph,
            legend: legend
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
        
        var controls = new RenderControls({
            element: $container.find('.side_panel')[0],
            graph: graph
        });

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
        initChart($(this));
    });

    $('body').on('click', '.js-find-target', function() {
        var $self = $(this);

        var $target = $self.parent().find('input.' + $self.attr('for'));
        
        $target.click();

        return false;
    })
});