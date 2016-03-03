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
    $scope.gaugeLimit = 500;

    // variables for comparison
    $scope.avgPerformance1 = '--';
    $scope.distributionData1 = [];
    $scope.capacity1 = '--';
    $scope.reliability1 = '--';
    $scope.avgPerformance2 = '--';
    $scope.distributionData2 = [];
    $scope.capacity2 = '--';
    $scope.reliability2 = '--';

    $scope.selectedService1 = {
        Id: 0
    };

    $scope.selectedService2 = {
        Id: 0
    };

    $scope.allServices = [];

    $scope.selectService = function () {
        console.log($scope.selectedService1);
        console.log($scope.selectedService2);
    }

    $http.get('/Service/Services').success(function (response) {
        $scope.allServices = response;
        $scope.selectedService1 = $scope.allServices[0];
        $scope.selectedService2 = $scope.allServices[1];
        console.log($scope.allServices);
    });

    var promise = undefined;

    var average = function (res, limit) {
        var sum = 0;

        for (var i = 0; i < limit; i++)
            sum += Number(res[i].Duration);

        return parseFloat(sum / limit).toFixed(1);
    }

    $scope.runTest = function () {
        $scope.showRunButton = true;
        $http.get("/Service/RunTest/" + $window.MODEL_ID).success(function () { });
        $scope.start();
    };

    $scope.call = function (full) {
        $http.get("/Service/Json/" + $window.MODEL_ID)
    	  .success(function (response) {
    	      // response time received
    	      var res = response.Results;

    	      // pupulate gauge with last value
    	      $scope.valueGauge = Number(res[res.length - 2].Duration);

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
    	          $scope.avgPerformance = parseFloat(sum / res.length).toFixed(1);

    	          // reliability
    	          $scope.reliability = parseFloat(sum).toFixed(1);

    	          // capacity
    	          $scope.capacity = res.length;

    	          // distribution
    	          setDistributionLabel(res, $scope.distributionBase);
    	          $scope.distributionData.push(distribution(res, $scope.distributionBase));
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

    var distribution = function (res, base) {
        if (res && base > 1) {
            var dataSet = [];
            var power = 1;
            var dataPoint = Math.pow(base, power);

            while (dataPoint < res.length) {
                dataSet.push(average(res, dataPoint));
                power++;
                dataPoint = Math.pow(base, power);
            }

            return dataSet;
        }
    }

    var setDistributionLabel = function (res, base) {
        var power = 1;

        if (base < 2) {
            return;
        }

        var dataPoint = Math.pow(base, power);

        // reset distribution label
        $scope.distributionLabels = [];

        while (dataPoint < res.length) {
            $scope.distributionLabels.push(dataPoint);
            power++;
            dataPoint = Math.pow(base, power);
        }
    }

    $scope.compare = function () {
        // clear data sets
        $scope.distributionData1 = [];
        $scope.distributionData2 = [];

        $http.get("/Service/Json/" + $scope.selectedService1.Id)
    	  .success(function (response) {
    	      // response time received
    	      var res = response.Results;

    	      // set url
    	      $scope.url1 = response.Url;

    	      // calculate average
    	      var sum = 0;
    	      for (var i = 0; i < res.length; i++)
    	          sum += Number(res[i].Duration);
    	      $scope.avgPerformance1 = parseFloat(sum / res.length).toFixed(1);

    	      // reliability
    	      $scope.reliability1 = parseFloat(sum).toFixed(1);

    	      // capacity
    	      $scope.capacity1 = res.length;

    	      // distribution
    	      setDistributionLabel(res, $scope.compareBase);
    	      $scope.distributionData1.push(distribution(res, $scope.compareBase));
    	  });

        $http.get("/Service/Json/" + $scope.selectedService2.Id)
          .success(function (response) {
              // response time received
              var res = response.Results;

              // set url
              $scope.url2 = response.Url;

              // calculate average
              var sum = 0;
              for (var i = 0; i < res.length; i++)
                  sum += Number(res[i].Duration);
              $scope.avgPerformance2 = parseFloat(sum / res.length).toFixed(1);

              // reliability
              $scope.reliability2 = parseFloat(sum).toFixed(1);

              // capacity
              $scope.capacity2 = res.length;

              // distribution
              $scope.distributionData2.push(distribution(res, $scope.compareBase));
          });
    }
}]);