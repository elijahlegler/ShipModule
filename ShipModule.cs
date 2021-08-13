using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
//using SupplyChainCloud.Api.Client; //old API
using SupplyChainCloud.Api.Client.v1_1;
using System.Xml;

namespace ShipModule
{
    public partial class ShipModule : Form
    {
        public ShipModule()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            string input = this.txtContainerID.Text;

            if (!String.IsNullOrEmpty(input))
            {
                GetData(input);
            }
            else
            {
                MessageBox.Show("Please enter a value for container ID", "Missing Value!");
            }

            this.txtContainerID.Text = "";
            this.txtContainerID.Focus();
        }

        private void GetData(string containerID)
        {
            try
            {   
                //NEED TO ENCRYPT CONNECTION STRING
                String connectionString = ConfigurationManager.ConnectionStrings["ProductionDB"].ConnectionString;

                //Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                //config.ConnectionStrings.SectionInformation.ProtectSection("NONE");

                DataTable containerTable = new DataTable();

                var conn = new SqlConnection(connectionString);
                using (var cmd = new SqlCommand("BHS_ShipModule_GetInfo", conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    cmd.Parameters.Add(new SqlParameter("@Container_Id", containerID));
                    cmd.CommandType = CommandType.StoredProcedure;
                    adapter.Fill(containerTable);
                }


                if (containerTable.Rows.Count > 0)
                {
                    SendJSON(containerTable);
                }
                else
                {
                    MessageBox.Show("Please enter a valid container ID.", "ERROR");
                }

            }

            catch (Exception es)
            {
                MessageBox.Show(es.Message);
            }
        }

        public static void PrintLabel(byte[] data)
        {
            try
            {
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.NoDelay = true;

                IPAddress ipAddress = IPAddress.Parse("10.12.200.204");
                IPEndPoint ipEndpoint = new IPEndPoint(ipAddress, 9100);
                clientSocket.Connect(ipEndpoint);

                clientSocket.Send(data);
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void UpdateTrackingNumber(string trackNumber, string contID)
        {
            String connectionString = ConfigurationManager.ConnectionStrings["ProductionDB"].ConnectionString;

            try
            {
                var conn = new SqlConnection(connectionString);
                using (var cmd = new SqlCommand("BHS_ShipModule_UpdateTracking", conn))
                {
                    cmd.Parameters.Add(new SqlParameter("@TRACKING_NUMBER", trackNumber));
                    cmd.Parameters.Add(new SqlParameter("@CONTAINER_ID", contID));
                    cmd.CommandType = CommandType.StoredProcedure;
                    conn.Open();
                    int result = cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while trying to update the tracking number");
                Console.WriteLine(ex);
            }
        }

        private static async void SendJSON(DataTable container)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://shipapi.thesccloud.com/")
            };

            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("scc-api-key", "A2F381FB-76F5-4269-AAD0-235F694DF387");

            var client = new SupplyChainCloudApiClientV1_1(httpClient);

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var resp = await client.GetCarrierServicesEnabledAsync();
            Console.WriteLine(JsonConvert.SerializeObject(resp, Newtonsoft.Json.Formatting.Indented));

            DataRow containerRow = container.Rows[0];

            var dto = new RateShipRequestDto()
            {
                //FreightTerm = FreightTerm.ThirdParty,
                IncludeTransactionXmls = true,
                Carriers = new List<Carrier>()
                {
                    new Carrier()
                    {
                         Name = "FedEx",
                         CarrierServiceCodes = new List<string>()
                         {
                            containerRow["CarrierServiceCode"].ToString()
                         },
                          AccountAssignment = new AccountAssignment()
                          {
                               AccessKey = "Ft8srdTIB036tJsU",
                               AccountNumber = "338916280",                    //production credentials, have to use these if IsProduction is uncommented
                               Password = "sXA0rObCTKceg2RfCH3O9XjfT",
                               UserName = "250982139" 

                              /*AccessKey = "nwAfTu7gGo5eCUnp",
                              AccountNumber = "510087860",                    // test credentials
                              Password = "qpr7wp4yjk7xlusttMAQc76F5",
                              UserName = "114038265"*/
                          },
                          IsProduction = true
                    }
                },
                Shipper = new Address()
                {
                    Name = containerRow["sfName"].ToString(),
                    City = containerRow["sfCity"].ToString(),
                    StateProvinceCode = containerRow["sfStateProvinceCode"].ToString(),
                    PostalCode = containerRow["sfPostalCode"].ToString(),
                    AddressLines = new List<string>() { containerRow["sfAddressLine1"].ToString() },
                    Country = containerRow["sfCountry"].ToString(),
                    CountryCode = containerRow["sfCountryCode"].ToString(),
                    PhoneNumber = containerRow["sfPhoneNum"].ToString()
                },
                ShipFrom = new Address()
                {
                    Name = containerRow["sfName"].ToString(),
                    City = containerRow["sfCity"].ToString(),
                    StateProvinceCode = containerRow["sfStateProvinceCode"].ToString(),
                    PostalCode = containerRow["sfPostalCode"].ToString(),
                    AddressLines = new List<string>() { containerRow["sfAddressLine1"].ToString(), containerRow["sfAddressLine2"].ToString(), containerRow["sfAddressLine3"].ToString(), },
                    Country = containerRow["sfCountry"].ToString(),
                    CountryCode = containerRow["sfCountryCode"].ToString(),
                    PhoneNumber = containerRow["sfPhoneNum"].ToString()
                },
                ShipTo = new Address()
                {
                    Name = containerRow["stName"].ToString(),
                    City = containerRow["stCity"].ToString(),
                    StateProvinceCode = containerRow["stStateProvinceCode"].ToString(),
                    PostalCode = containerRow["stPostalCode"].ToString(),
                    AddressLines = new List<string>() { containerRow["stAddressLine1"].ToString(), containerRow["stAddressLine2"].ToString(), containerRow["stAddressLine3"].ToString(), },
                    Country = containerRow["stCountry"].ToString(),
                    CountryCode = containerRow["stCountry"].ToString(),
                    PhoneNumber = containerRow["stPhoneNum"].ToString(),
                    AttentionTo = containerRow["stAttentionTo"].ToString(),
                    Company = containerRow["stCompany"].ToString()
                },
                Shipment = new Shipment()
                {
                    OrderNumber = containerRow["OrderNumber"].ToString(),
                    Accessorials = new List<AppliedAccessorial>() { },
                    Containers = new List<SupplyChainCloud.Api.Client.v1_1.Container>()
                    {
                         new SupplyChainCloud.Api.Client.v1_1.Container()
                         {
                              ContainerId = containerRow["ContainerID"].ToString(),
                              ItemName = containerRow["ItemName"].ToString(),
                              Quantity = Convert.ToDecimal(containerRow["Quantity"]),
                              Length = Convert.ToDecimal(containerRow["Length"]),
                              Width = Convert.ToDecimal(containerRow["Width"]),
                              Height = Convert.ToDecimal(containerRow["Height"]),
                              Weight = Convert.ToDecimal(containerRow["Weight"])
                         }
                    }
                },
            };

            //add in any accessorials (dry ice for now)
            foreach (var containe in dto.Shipment.Containers)
            {
                if (containerRow["accessorial"].ToString() == "Dry Ice Flag")
                {
                    var accessorial = new AppliedAccessorial
                    {
                        AccessorialType = AccessorialType.DryIce,
                        ContainerId = containe.ContainerId
                    };

                    accessorial.AppliedAccessorialValues = new List<AppliedAccessorialValue>
                    {
                        new AppliedAccessorialValue()
                        {
                             Key = "DryIceWeightKg",
                             Value = containerRow["accessorialValue"].ToString()
                        }
                    };

                    dto.Shipment.Accessorials.Add(accessorial);
                }
            }

            //add in 3rd party account number if third party billing
            if (containerRow["freightTerms"].ToString() == "3RD")
            {
                dto.FreightTerm = FreightTerm.ThirdParty;
                dto.ThirdPartyAccountNumber = containerRow["third_party_acct_num"].ToString();
                dto.Carriers[0].AccountAssignment.AdditionalAccountNumber = containerRow["third_party_acct_num"].ToString();
            }

            var json = JsonConvert.SerializeObject(dto, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(@"C:\BHS\testJSONObject.txt", json);

            var success = false;
            string output;
            ShipResponseDto response = new ShipResponseDto();
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                response = await client.PostShipAsync(dto);

                output = JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);

                //check if the response is valid, if not pass back the error from FedEx Service
                if (response.ShipResultDto.ShipResult.ContainerShipResults.Count() > 0)
                {
                    success = true;
                }
                else
                {
                    MessageBox.Show("Unable to print shipping Label. \n Error: " + response.ShipResultDto.ParcelErrors[0].Message.ToString());
                    Application.Exit();

                }
            }
            catch (ApiException<ApiMessageDto> ex)
            {
                output = JsonConvert.SerializeObject(ex.Result, Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                output = JsonConvert.SerializeObject(ex, Newtonsoft.Json.Formatting.Indented);
            }

            string contID = containerRow["ContainerID"].ToString();

            File.WriteAllText(@"C:\BHS\FedExResponses\Response_" + contID + ".txt", output);
            
            if (success == true)
            {
                try
                {
                    //get zpl label and decode it from base64
                    byte[] data = Convert.FromBase64String(response.ShipResultDto.ShipResult.ContainerShipResults[0].Documents[0].Data.ToString());
                    string decodedString = Encoding.UTF8.GetString(data);
                    string trackingNumber = response.ShipResultDto.ShipResult.ContainerShipResults[0].TrackingNumber.ToString();
                    UpdateTrackingNumber(trackingNumber, contID);

                    PrintLabel(data);
                    File.WriteAllText(@"C:\BHS\ShipLabels\Label_" + contID + ".lbl", decodedString);
                    MessageBox.Show("Shipping Label Printed!");


                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    MessageBox.Show("An error occurred while attempting to print the label!");
                }
            }
        }

    }
}
