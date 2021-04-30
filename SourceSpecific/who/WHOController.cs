﻿using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Serilog;

namespace DataHarvester.who
{
    public class WHOController
    {
        ILogger _logger;
        IMonitorDataLayer _mon_repo;
        IStorageDataLayer storage_repo;
        WHOProcessor processor;
        Source source;
        int harvest_type_id;
        int harvest_id;

        public WHOController(ILogger logger, IMonitorDataLayer mon_repo, IStorageDataLayer _storage_repo,
                             Source _source, int _harvest_type_id, int _harvest_id)
        { 
            _logger = logger;
            _mon_repo = mon_repo;
            storage_repo = _storage_repo;
            processor = new WHOProcessor(storage_repo, mon_repo, logger);
            source = _source;
            harvest_type_id = _harvest_type_id;
            harvest_id = _harvest_id;
        }


        public int? LoopThroughFiles()
        {
            // Loop through the available records a chunk at a time (may be 1 for smaller rexord sources)
            // First get the total number of records in the system for this source
            // Set up the outer limit and get the relevant records for each pass

            XmlSerializer serializer = new XmlSerializer(typeof(WHORecord));
            int total_amount = _mon_repo.FetchFileRecordsCount(source.id, "study", harvest_type_id);
            int chunk = 100;
            int k = 0;
            for (int m = 0; m < total_amount; m += chunk)
            {
                // if (k > 50) break; // for testing...
                IEnumerable<StudyFileRecord> file_list = _mon_repo
                        .FetchStudyFileRecordsByOffset(source.id, m, chunk, harvest_type_id);
                int n = 0; string filePath = "";
                foreach (StudyFileRecord rec in file_list)
                {
                    // if (k > 50) break; // for testing...
                    n++; k++;
                    filePath = rec.local_path;
                    if (File.Exists(filePath))
                    {
                        string inputString = "";
                        using (var streamReader = new StreamReader(filePath, System.Text.Encoding.UTF8))
                        {
                            inputString += streamReader.ReadToEnd();
                        }

                        StringReader rdr = new StringReader(inputString);
                        WHORecord studyEntry = (WHORecord)serializer.Deserialize(rdr);

                        // break up the file into relevant data classes
                        Study s = processor.ProcessData(studyEntry, rec.last_downloaded);

                        // store the data in the database			  
                        processor.StoreData(s, source.db_conn);

                        // update file record with last processed datetime
                        // (if not in test mode)
                        if (harvest_type_id != 3)
                        {
                            _mon_repo.UpdateFileRecLastHarvested(rec.id, "study", harvest_id);
                        }
                    }
                }

                if (k % 100 == 0) _logger.Information("Records harvested: " + k.ToString());
            }

            return k;
        }
    }
}
