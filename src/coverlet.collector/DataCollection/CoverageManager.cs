﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using coverlet.collector.Resources;
using Coverlet.Collector.Utilities;
using Coverlet.Collector.Utilities.Interfaces;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;

namespace Coverlet.Collector.DataCollection
{
  /// <summary>
  /// Manages coverlet coverage
  /// </summary>
  internal class CoverageManager
  {
    private readonly Coverage _coverage;
    private readonly ICoverageWrapper _coverageWrapper;
    private readonly ISourceRootTranslator _sourceRootTranslator;
    public IReporter[] Reporters { get; }

    public CoverageManager(CoverletSettings settings, TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, ICoverageWrapper coverageWrapper,
                           IInstrumentationHelper instrumentationHelper, IFileSystem fileSystem, ISourceRootTranslator sourceRootTranslator, ICecilSymbolHelper cecilSymbolHelper)
        : this(settings,
        settings.ReportFormats.Select(format =>
        {
          var reporterFactory = new ReporterFactory(format);
          if (!reporterFactory.IsValidFormat())
          {
            eqtTrace.Warning($"Invalid report format '{format}'");
            return null;
          }
          else
          {
            return reporterFactory.CreateReporter();
          }
        }).Where(r => r != null).ToArray(),
        new CoverletLogger(eqtTrace, logger),
        coverageWrapper, instrumentationHelper, fileSystem, sourceRootTranslator, cecilSymbolHelper)
    {
    }

    public CoverageManager(CoverletSettings settings, IReporter[] reporters, ILogger logger, ICoverageWrapper coverageWrapper,
                           IInstrumentationHelper instrumentationHelper, IFileSystem fileSystem, ISourceRootTranslator sourceRootTranslator, ICecilSymbolHelper cecilSymbolHelper)
    {
      // Store input vars
      Reporters = reporters;
      _coverageWrapper = coverageWrapper;
      _sourceRootTranslator = sourceRootTranslator;
      // Coverage object
      _coverage = _coverageWrapper.CreateCoverage(settings, logger, instrumentationHelper, fileSystem, sourceRootTranslator, cecilSymbolHelper);
    }

    /// <summary>
    /// Instrument modules
    /// </summary>
    public void InstrumentModules()
    {
      try
      {
        // Instrument modules
        _coverageWrapper.PrepareModules(_coverage);
      }
      catch (Exception ex)
      {
        string errorMessage = string.Format(Resources.InstrumentationException, CoverletConstants.DataCollectorName);
        throw new CoverletDataCollectorException(errorMessage, ex);
      }
    }

    /// <summary>
    /// Gets coverlet coverage reports
    /// </summary>
    /// <returns>Coverage reports</returns>
    public IEnumerable<(string report, string fileName)> GetCoverageReports()
    {
      // Get coverage result
      CoverageResult coverageResult = GetCoverageResult();
      return GetCoverageReports(coverageResult);
    }

    /// <summary>
    /// Gets coverlet coverage result
    /// </summary>
    /// <returns>Coverage result</returns>
    private CoverageResult GetCoverageResult()
    {
      try
      {
        return _coverageWrapper.GetCoverageResult(_coverage);
      }
      catch (Exception ex)
      {
        string errorMessage = string.Format(Resources.CoverageResultException, CoverletConstants.DataCollectorName);
        throw new CoverletDataCollectorException(errorMessage, ex);
      }
    }

    /// <summary>
    /// Gets coverage reports from coverage result
    /// </summary>
    /// <param name="coverageResult">Coverage result</param>
    /// <returns>Coverage reports</returns>
    private IEnumerable<(string report, string fileName)> GetCoverageReports(CoverageResult coverageResult)
    {
      try
      {
        return Reporters.Select(reporter => (reporter.Report(coverageResult, _sourceRootTranslator), Path.ChangeExtension(CoverletConstants.DefaultFileName, reporter.Extension)));
      }
      catch (Exception ex)
      {
        string errorMessage = string.Format(Resources.CoverageReportException, CoverletConstants.DataCollectorName);
        throw new CoverletDataCollectorException(errorMessage, ex);
      }
    }
  }
}
