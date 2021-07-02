﻿using Dapper;
using Npgsql;
using System.Collections.Generic;

namespace DataHarvester
{
    class ExpectedDataBuilder
    {
        string _db_conn;

        public ExpectedDataBuilder(string db_conn)
        {
            _db_conn = db_conn;
        }


        public void Execute_SQL(string sql_string)
        {
            using (var conn = new NpgsqlConnection(_db_conn))
            {
                conn.Execute(sql_string);
            }
        }


        private void LoadStudyData(string study_id)
        {
            string sp_call = "call expected.study_" + study_id + "();";
            using (var conn = new NpgsqlConnection(_db_conn))
            {
                conn.Execute(sp_call);
            }
        }


        public void InitialiseTestStudiesList()
        {
            string sql_string = @"DROP TABLE IF EXISTS expected.source_studies;
            create table expected.source_studies as
            select * from mon_sf.source_data_studies
            where for_testing = true;";

            Execute_SQL(sql_string);

            // Initialise expected studies table with registry ids from source studies table

            sql_string = @"insert into expected.studies(sd_sid)
            select sd_id from 
            expected.source_studies 
            order by source_id, sd_id;";

            Execute_SQL(sql_string);
        }

        public void LoadInitialInputTables()
        {
            // clinicaltrials.gov studies

            LoadStudyData("nct00002516");
            LoadStudyData("nct00023244");
            LoadStudyData("nct00051350");

            LoadStudyData("nct00094302");
            LoadStudyData("nct00200967");
            LoadStudyData("nct00433329");

            LoadStudyData("nct01727258");
            LoadStudyData("nct01973660");
            LoadStudyData("nct02243202");

            LoadStudyData("nct02318992");
            LoadStudyData("nct02441309");
            LoadStudyData("nct02449174");

            LoadStudyData("nct02562716");
            LoadStudyData("nct02609386");
            LoadStudyData("nct02798978");
            LoadStudyData("nct02922075");
            LoadStudyData("nct03050593");
            LoadStudyData("nct03076619");

            LoadStudyData("nct03167125");
            LoadStudyData("nct03226236");
            LoadStudyData("nct03631199");

            LoadStudyData("nct03786900");
            LoadStudyData("nct04406714");
            LoadStudyData("nct04419571");

            // biolincc studies

            LoadStudyData("acrn_bags");
            LoadStudyData("acrn_large");
            LoadStudyData("baby_hug");
            LoadStudyData("omni_heart");
            LoadStudyData("topcat");

            // yoda studies

            LoadStudyData("0a663fd89b1c34636e462d011f1a97d7");
            LoadStudyData("5f1f01152c98133141e01ce922814433");
            LoadStudyData("85d4da6dbbfad175ca83961171be5ad7");
            LoadStudyData("213154085c0a14f6998432e313a7cd86");
            LoadStudyData("b534c4ec25b421860e600ed8b3131184");

            // euctr studies

            LoadStudyData("2004_001569_16");
            LoadStudyData("2009_011622_34");
            LoadStudyData("2012_000615_84");
            LoadStudyData("2013_001036_22");
            LoadStudyData("2015_000556_14");
            LoadStudyData("2018_001547_32");

            // isctrn studies

            LoadStudyData("isrctn00075564");
            LoadStudyData("isrctn16535250");
            LoadStudyData("isrctn59589587");
            LoadStudyData("isrctn82138287");
            LoadStudyData("isrctn88368130");

            // WHO studies

            LoadStudyData("actrn12616000771459");
            LoadStudyData("actrn12620001103954");
            LoadStudyData("chictr_ooc_16010171");
            LoadStudyData("chictr_poc_17010431");
            LoadStudyData("ctri_2017_03_008228");
            LoadStudyData("ctri_2019_06_019509");
            LoadStudyData("drks00011324");
            LoadStudyData("jprn_jrcts012180017");
            LoadStudyData("jprn_umin000024722");
            LoadStudyData("jprn_umin000028075");
            LoadStudyData("lbctr2019070214");
            LoadStudyData("nl8683");
            LoadStudyData("ntr1437");
            LoadStudyData("per_015_19");
            LoadStudyData("tctr20161221005");
        }


        public void CalculateAndAddOIDs()
        {
            // oids have to be calculated for each of the data objects
            // of the manually derived studies - in the same way as 
            // in normal data extraction

            MD5Helpers hh = new MD5Helpers();

            // get each data object - calulate oid as the 
            // hash of the sd_sid and the object's display name
            string sql_string = @"select sd_sid, display_title
                                from expected.data_objects";

            IEnumerable<DataObjectBasics> object_ids = null;

            using (var conn = new NpgsqlConnection(_db_conn))
            {
                object_ids = conn.Query<DataObjectBasics>(sql_string);
            }

            foreach (DataObjectBasics d in object_ids)
            {
                string ids = d.sd_sid + d.display_title;
                string sd_oid = hh.CreateMD5(ids);

                sql_string = @"Update expected.data_objects 
                             set sd_oid = '" + sd_oid + @"'
                             where sd_sid = '" + d.sd_sid + @"'
                             and display_title = '" + d.display_title + @"';";

                Execute_SQL(sql_string);
            }

            // update the object attributes that share the 
            // same study and sequence number

            Update_sd_oid("object_datasets");
            Update_sd_oid("object_dates");
            Update_sd_oid("object_instances");
            Update_sd_oid("object_titles");
            Update_sd_oid("object_contributors");
            Update_sd_oid("object_topics");
            Update_sd_oid("object_descriptions");
            Update_sd_oid("object_identifiers");
            Update_sd_oid("object_db_links");
            Update_sd_oid("object_publication_types");
            Update_sd_oid("object_rights");
            //Update_sd_oid("object_hashes");
        }


        private void Update_sd_oid (string table_name)
        {
            string sql_string = @"Update expected." + table_name +  @" b
                             set sd_oid = d.sd_oid 
                             from expected.data_objects d
                             where b.sd_sid = d.sd_sid 
                             and b.seq_num = d.seq_num;";

            Execute_SQL(sql_string);

        }

        private class DataObjectBasics
        { 
            public string sd_sid { get; set; }
            public string display_title { get; set; }
        }

    }
}
