﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace DataHarvester.pubmed
{
    class PubmedController
    {
        DataLayer repo;
        LoggingDataLayer logging_repo;
        PubmedProcessor processor;
        Source source;
        int harvest_id;
        int harvest_type_id;

        public PubmedController(int _harvest_id, Source _source, DataLayer _repo, LoggingDataLayer _logging_repo, int _harvest_type_id)
        {
            source = _source;
            processor = new PubmedProcessor();
            repo = _repo;
            logging_repo = _logging_repo;
            harvest_id = _harvest_id;
            harvest_type_id = _harvest_type_id;
        }


        public int? LoopThroughFiles()
        {
            // ***************************************************
            // set up pp local extraction errors folder .....
            // or better to use centtral sf mon....
            // **************************************************

            string fileBase = source.local_folder;

            // harvest_type_id can be 1 (all), 2 (use cutoff date) or 3 (harvest_type_id only 'incomplete' files)
            int total_amount = logging_repo.FetchFileRecordsCount(source.id, "object", harvest_type_id);
            int chunk = 1000;
            int k = 0;

            for (int m = 0; m < total_amount; m += chunk)
            {
                IEnumerable<ObjectFileRecord> file_list = logging_repo.FetchObjectFileRecordsByOffset(source.id, m, chunk, harvest_type_id);
                int n = 0; string filePath = "";
                foreach (ObjectFileRecord rec in file_list)
                {
                    n++; k++;
                    filePath = rec.local_path;
                    if (File.Exists(filePath))
                    {
                        XmlDocument xdoc = new XmlDocument();
                        xdoc.Load(filePath);
                        CitationObject c = processor.ProcessData(logging_repo, rec.sd_id, xdoc, rec.last_downloaded, harvest_id);
                        processor.StoreData(repo, c);

                        // update file record with last processed datetime
                        logging_repo.UpdateFileRecLastHarvested(rec.id, "object", harvest_id);
                    }

                    if (k % 100 == 0) StringHelpers.SendFeedback(k.ToString());
                }

                // if (k > 9990) break;  // testing only
            }

            return k;
        }

        public void DoPubMedPostProcessing()
        {
            PubmedPostProcBuilder ppb = new PubmedPostProcBuilder(repo.ConnString, source);
            ppb.EstablishContextForeignTables(repo.Username, repo.Password);

            ppb.ObtainPublisherNames();
            ppb.UpdatePublisherOrgIds();
            ppb.UpdateIdentifierPublisherData();
            ppb.CreateDataObjectsTable();
            ppb.CreateTotalLinksTable();

            ppb.DropContextForeignTables();
        }

    }
}
