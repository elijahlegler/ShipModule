set ANSI_NULLS OFF
set QUOTED_IDENTIFIER OFF
GO

ALTER PROCEDURE [dbo].[BHS_ShipModule_GetInfo] 
	@Container_Id	nvarchar(25)
AS

set nocount on

DECLARE @INT_CONT_NUM numeric(9,0);
DECLARE @INT_SHIP_NUM numeric(9,0);
DECLARE @ACCESSORIAL nvarchar(25);
DECLARE @ACCESSORIAL_VALUE nvarchar(50);
DECLARE @ThirdPartyAccount nvarchar(25);

SELECT @INT_CONT_NUM = internal_container_num from shipping_container where container_id = @Container_Id
SELECT @INT_SHIP_NUM = internal_shipment_num from shipping_container where container_id = @Container_Id

--accessorials can be based on int_ship_num or int_cont_num in the table so we test to see which was used for this shipment
--each of next 3 case statements reflect that 
SELECT @ACCESSORIAL = 
CASE
 WHEN (select top 1 accessorial_code from shipment_accessorials where INTERNAL_NUM = @INT_SHIP_NUM and accessorial_code = 'Dry Ice Flag') is not null 
  THEN (select top 1 accessorial_code from shipment_accessorials where INTERNAL_NUM = @INT_SHIP_NUM and accessorial_code = 'Dry Ice Flag')
 ELSE (select top 1 accessorial_code from shipment_accessorials where INTERNAL_NUM = @INT_CONT_NUM and accessorial_code = 'Dry Ice Flag')
END

SELECT @ACCESSORIAL_VALUE = 
CASE
 WHEN (select top 1 isnull(((value/100)/2.2046), 0) from shipment_accessorials where INTERNAL_NUM = @INT_SHIP_NUM and accessorial_code = 'Dry Ice Flag' and accessorial_sub_code = 'Dry Ice Weight') is not null
  THEN (select top 1 isnull(((value/100)/2.2046), 0) from shipment_accessorials where INTERNAL_NUM = @INT_SHIP_NUM and accessorial_code = 'Dry Ice Flag' and accessorial_sub_code = 'Dry Ice Weight')
 ELSE (select top 1 isnull(((value/100)/2.2046), 0) from shipment_accessorials where INTERNAL_NUM = @INT_CONT_NUM and accessorial_code = 'Dry Ice Flag' and accessorial_sub_code = 'Dry Ice Weight')
END

--SELECT @ThirdPartyAccount = 
--CASE
-- WHEN (select top 1 value from shipment_accessorials where internal_shipment_num = @INT_SHIP_NUM and accessorial_code = '3rd Pty Billing' and accessorial_sub_code = '3rd Pty Billing Acct') is not null
--  THEN (select top 1 value from shipment_accessorials where internal_shipment_num = @INT_SHIP_NUM and accessorial_code = '3rd Pty Billing' and accessorial_sub_code = '3rd Pty Billing Acct')
-- ELSE (select value from shipment_accessorials where internal_container_num = @INT_CONT_NUM and accessorial_code = '3rd Pty Billing' and accessorial_sub_code = '3rd Pty Billing Acct')
--END

-- Container Data
select sh.Shipment_ID 'OrderNumber',
		sc.Container_ID 'ContainerId',
		(select top 1 Item from shipping_container where Parent = sc.Internal_Container_Num)'ItemName',
		1 'Quantity',
		sc.Length 'Length',
		sc.Width 'Width',
		sc.Height 'Height',
		sc.Weight 'Weight',

		sh.Carrier 'CarrierName',
		CASE WHEN sh.carrier_service = 'GROUND BUS' THEN 'FEDEX_GROUND'
			 WHEN sh.carrier_service = 'FIRST PRIORITY OVERNIGHT' THEN 'FIRST_OVERNIGHT'
			 WHEN sh.carrier_service = 'STANDARD OVERNIGHT' THEN 'STANDARD_OVERNIGHT'
			 WHEN sh.carrier_service = 'PRIORITY OVERNIGHT' THEN 'PRIORITY_OVERNIGHT'
			 WHEN sh.carrier_service = '2DAY' THEN 'FEDEX_2_DAY'
			 WHEN sh.carrier_service = 'EXPRESS SAVER' THEN 'FEDEX_EXPRESS_SAVER'
			 ELSE 'INVALID CARRIER'
		END 'CarrierServiceCode',
		sh.freight_terms 'freightTerms',
		--@ThirdPartyAccount 'third_party_acct_num', --update 07.12.2021 Sentry no longer uses accessorials for third part billing, instead they use cust_category2
		ISNULL(sh.customer_category2, '') 'third_party_acct_num',

		sh.Ship_To_Name 'stName',
		sh.Ship_To_City 'stCity',
		sh.Ship_To_State 'stStateProvinceCode',
		sh.Ship_To_Postal_Code 'stPostalCode',
		sh.Ship_To_Address1 'stAddressLine1',
		ISNULL(sh.SHIP_TO_ADDRESS2, '') 'stAddressLine2',
		ISNULL(sh.SHIP_TO_ADDRESS3, '') 'stAddressLine3',
		sh.Ship_To_Country 'stCountry',
		left(sh.SHIP_TO_PHONE_NUM, 20) 'stPhoneNum',
		sh.Ship_To_Name 'stAttentionTo', --Attention To is the field that (if sent in the request) gets populated on the first line of the shipping label, where Sentry wants the Ship To Name
		sh.SHIP_TO_ATTENTION_TO 'stCompany', --company is the field that gets put as the second line on the shipping label, where Sentry wants the Attention To field 

		sh.Customer_Name 'sfName',
		w.City 'sfCity',
		w.State 'sfStateProvinceCode',
		w.Postal_Code 'sfPostalCode',
		w.Returns_Address1 'sfAddressLine1',
		ISNULL(w.RETURNS_ADDRESS2, '') 'sfAddressLine2',
		ISNULL(w.RETURNS_ADDRESS3, '') 'sfAddressLine3',
		w.Returns_Country 'sfCountry',
		w.Returns_Country 'sfCountryCode',
		w.PHONE_NUM 'sfPhoneNum',

		@ACCESSORIAL 'accessorial',
		@ACCESSORIAL_VALUE 'accessorialValue'

from shipping_container sc
	join shipment_header sh
		on sh.internal_shipment_num = sc.internal_shipment_num
	join WAREHOUSE w
		on w.warehouse = sh.warehouse
where Container_Id = @Container_Id










