!function ($) {
    "use strict";

    var Chart = function (type, element, options) {
        this.init(type, element, options);
        this.series = [{ color: '', name: '', data: [{ x: 0, y: 0 }] }];
    };

    Chart.prototype = {
        constructor: Chart,        

        init: function(type, element, options) {

            this.$element = $(element);
            this.type = type;
            this.options = this.getOptions(options);
            this.palette = new Rickshaw.Color.Palette({ scheme: this.options.colorscheme });
            this.getSeries(this.initChart.bind(this));            
        },

        initChart:function(newSeries) {
            var ticksTreatment = 'glow';

            var $el = this.$element;             

            if (newSeries.length)
                this.series.shift();

            for (var i = 0; i < newSeries.length; i++) {
                this.series[i] = newSeries[i];
            }
           
            this.initElements();

            var width = $('body').width() - 80;
            $el.css({ width: width + 'px' });
            
            this.graph = new Rickshaw.Graph({
                element: this.$chart[0],
                width: width - 500,
                height: 250,
                renderer: 'area',
                stroke: true,
                preserve: true,
                series: this.series
            });

            this.graph.render();
            
            if (newSeries.length)
                $el.fadeIn();

            this.hoverDetail = new Rickshaw.Graph.HoverDetail({
                graph: this.graph,
                xFormatter: function (x) { var date = new moment(x * 1000); return date.format('LLLL'); }
            });           

            if (this.options.annotations) {
                this.annotator = new Rickshaw.Graph.Annotate({
                    graph: this.graph,
                    element: this.$timeline[0]
                });

                this.getAnnotations(this.addAnnotations.bind(this));                
            }

            this.legend = new Rickshaw.Graph.Legend({
                graph: this.graph,
                element: this.$legend[0]

            });

            this.shelving = new Rickshaw.Graph.Behavior.Series.Toggle({
                graph: this.graph,
                legend: this.legend
            });

            this.highlighter = new Rickshaw.Graph.Behavior.Series.Highlight({
                graph: this.graph,
                legend: this.legend
            });

            this.xAxis = new Rickshaw.Graph.Axis.Time({
                graph: this.graph,
                ticksTreatment: ticksTreatment,
                timeFixture: new Rickshaw.Fixtures.Time.Local()
            });

            this.xAxis.render();

            this.yAxis = new Rickshaw.Graph.Axis.Y({
                graph: this.graph,
                tickFormat: Rickshaw.Fixtures.Number.formatKMBT,
                ticksTreatment: ticksTreatment
            });

            this.yAxis.render();

            this.controls = new RenderControls({
                element: $el.find('.side_panel')[0],
                graph: this.graph
            });

            setInterval(function() {

                this.getSeries(function (series) {
                   
                    for (var i = 0; i < series.length; i++) {
                        if (this.series[i] && this.series[i].data)
                            this.series[i].data = series[i].data;
                        else {
                            this.series[i] = series[i];
                        }
                    }
                    
                    this.graph.update();
                    
                }.bind(this));

            }.bind(this), this.options.updateInterval);

            if (this.options.annotations) {

                setInterval(function () {
                    this.getAnnotations(this.addAnnotations.bind(this));
                }.bind(this), this.options.updateInterval);

            }

        },

        initElements: function () {
            var $el = this.$element;
            var options = this.options;

            var template = '<div class="chart-header"></div><table width="100%"><tr><td class="control"></td><td><div class="chart"></div></td><td><div class="legend"></div></td></tr></table>';

            $el.empty().append(template);

            this.$chart = $el.find('.chart');
            this.$legend = $el.find('.legend');
            this.$header = $el.find('.chart-header').text(options.title);

            $el.find('.control').append(options.controlTemplate);

            if (options.annotations) {
                this.$timeline = $('<div class="timeline"></div>').insertAfter(this.$chart);
            }
        },

        getSeries: function (callback) {
            var start = new Date();
            start = new Date(start.setHours(start.getHours() - 1)).toISOString();

            var stop = new Date().toISOString();

            $.getJSON(this.options.url, { start: start, stop: stop, expression: this.options.expression }, function(series) {

                series = this.seriesListConverter(series);
                
                callback(series);

            }.bind(this));
        },

        getAnnotations: function(callback) {
            var start = new Date();
            start = new Date(start.setHours(start.getHours() - 1)).toISOString();

            var stop = new Date().toISOString();

            $.getJSON(this.options.annotationUrl, { start: start, stop: stop }, callback.bind(this));
        },

        addAnnotations:function(annotations) {
            if (annotations) {

                for (var i = 0; i < annotations.length; i++) {
                    var annotation = annotations[i];
                    var time = annotation.timestamp;
                    var message = annotation.message;

                    this.annotator.add(parseInt(time), message);
                }

                this.$element.find('.annotation').empty();                

                this.annotator.update();
            }
        },

        seriesListConverter: function (data) {

            var series = [];            

            for (var i = 0; i < data.length; i++) {

                var points = [];

                for (var j = 0; j < data[i].dataPoints.length; j++) {
                    var point = {
                        y: data[i].dataPoints[j][0],
                        x: data[i].dataPoints[j][1] * 1
                    };
                    points[j] = point;
                }

                series[i] = {
                    data: points,
                    color: this.palette.color(),
                    name: data[i].target
                };
            }

            return series;
        },

        getOptions: function(options) {

            var result = $.extend({}, $.extend(true, {}, $.fn.chart.defaults), options);

            if (this.$element.data('url')) {
                result.url = this.$element.data('url');
            }

            if (this.$element.data('expression')) {
                result.expression = this.$element.data('expression');
            }

            if (this.$element.data('annotations')) {
                result.annotations = this.$element.data('annotations');
            }

            if (this.$element.data('annotations-url')) {
                result.annotationUrl = this.$element.data('annotations-url');
            }

            if (this.$element.data('title')) {
                result.title = this.$element.data('title');
            }

            if (this.$element.data('update-interval')) {
                result.updateInterval = this.$element.data('update-interval');
            }

            if (this.$element.data('color-scheme')) {
                result.colorscheme = this.$element.data('color-scheme');
            }

            return result;
        }

    };

    $.fn.chart = function (option) {

        return this.each(function () {

            var $this, data, options, type;
            type = 'rickshaw-chart';
            $this = $(this);
            data = $(this).data(type);
            options = typeof option === 'object' && option;

            if (!data) {
                $this.data(type, (data = new Chart(type, this, options)));
            }

            if (typeof option === 'string') {
                data['client_' + option]();
            }
        });
    };

    $.fn.chart.Constructor = Chart;

    $.fn.chart.defaults = {
        url: undefined,
        annotationUrl: undefined,
        expression: undefined,
        annotations: false,
        title: undefined,
        updateInterval: 10000,
        colorscheme:'spectrum14',
        controlTemplate: '<form class="side_panel"><section><div class=" renderer_form toggler"><input type="radio" name="renderer" class="area" value="area" checked><label for="area" class="js-find-target">area</label><input type="radio"  name="renderer" class="bar" value="bar"><label for="bar" class="js-find-target">bar</label><input type="radio" name="renderer" class="line" value="line"><label for="line" class="js-find-target">line</label><input type="radio" name="renderer" class="scatter" value="scatterplot"><label for="scatter" class="js-find-target">scatter</label></div></section><section><div class="offset_form"><label for="stack"><input type="radio" name="offset" class="stack" value="zero" checked><span>stack</span></label><label for="stream"><input type="radio" name="offset" class="stream" value="wiggle"><span>stream</span></label><label for="pct"><input type="radio" name="offset" class="pct" value="expand"><span>pct</span></label><label for="value"><input type="radio" name="offset" class="value" value="value"><span>value</span></label></div><div class="interpolation_form"><label for="cardinal" ><input type="radio" name="interpolation" class="cardinal" value="cardinal" checked><span>cardinal</span></label><label for="linear" ><input type="radio" name="interpolation" class="linear" value="linear"><span>linear</span></label><label for="step" ><input type="radio" name="interpolation" class="step" value="step-after"><span>step</span></label></div></section></form>'
    };

}(window.jQuery);

$(document).ready(function() {
    $('.chart_container').chart();
});