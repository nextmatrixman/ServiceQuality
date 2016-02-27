angular.module('app', ['charts.ng.justgage', 'chart.js']).controller('AppCtrl', ['$scope', '$interval', '$http', '$window', function ($scope, $interval, $http, $window) {
	$scope.performanceLabels = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12', '13', '14', '15', '16', '17', '18', '19', '20'];
  $scope.performanceSeries = ['Performance (ms)'];
  $scope.performanceData = [[]];
  $scope.avgPerformance = '--';
  $scope.distributionLabels = [];
  $scope.distributionSeries = ['Distribution (ms)'];
  $scope.distributionData = [];
  $scope.capacity = '--';
  $scope.reliability = '--';
  $scope.testurl = '';
  $scope.parameter = '';
  $scope.gaugeLimit = 1000;

	var promise = undefined;

	var average = function (res, limit) {
		var sum = 0;

		for (var i = 0; i < limit; i++)
		  sum += Number(res[i].Duration);

		return parseFloat(sum/limit).toFixed(1);
	}
	
	$scope.runTest = function () {
		$scope.showRunButton = true;
		$http.get("/Service/RunTest/" + $window.MODEL_ID).success(function () {});
		$scope.start();
	};

	$scope.call = function (full) {
		$http.get("/Service/Json/" + $window.MODEL_ID)
    	.success(function (response) {
    		// response time received
        var res = response.Results;

        // pupulate gauge with last value
        $scope.valueGauge = Number(res[res.length-2].Duration);
        
        // populate chart with last 20 entries
        var dataSet = [];
        for (var i = res.length - 21; i < res.length - 1; i++) {
        	if (res[i])
        		dataSet.push(res[i].Duration);
        }
        $scope.performanceData[0] = dataSet;

        if (full === 'full') {
        	// calculate average
        	var sum = 0;
        	for (var i = 0; i < res.length; i++)
		  			sum += Number(res[i].Duration);
		      $scope.avgPerformance = parseFloat(sum/res.length).toFixed(1);

		      // reliability
		      $scope.reliability = parseFloat(sum).toFixed(1);

		      // capacity
		      $scope.capacity = res.length;

		      // distribution
		      distribution(res);
        }
    });
	}

	$scope.start = function () {
		promise = $interval($scope.call, 1000);
	}

	$scope.stop = function () {
		if (promise) {
			$interval.cancel(promise);
			promise = undefined;
		}
	}

	var distribution = function (res) {
		if ($scope.distributionBase) {
			var dataSet = [];
			var base = $scope.distributionBase;
			var power = 1;
			var dataPoint = Math.pow(base, power);

			while (dataPoint < res.length) {
				$scope.distributionLabels.push(dataPoint);
				dataSet.push(average(res, dataPoint));
				power++;
				dataPoint = Math.pow(base, power);
			}

			$scope.distributionData.push(dataSet);
		}
	}
}]);