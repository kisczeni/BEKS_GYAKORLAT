# Beks_gyakorlat
Egy kliens és szerver program, valamint egy különálló Zabbix Agent alkalmazás




# ServerAndClient 

Az ServerAndClient projektben két külön alkalmazást implementáltam – egy klienst és egy szervert –, 
amelyek TCP/IP protokollrendszeren keresztül tudnak kommunikálni. A szerver koordinált világidőt (UTC) küld, amit a kliens szoftver,
 az időzónának megfelelő pontos időre vált át, annak ellenére is, ha a rendszeridő módosulna az adott eszközön. 



# ZabbixIntegration

A ZabbixIntegration egy olyan kliens alkalmazás (Agent), amely adatokat "gyűjt", amit időközönként egy valós idejű távoli szervernek továbbít.
Ezen kiszolgálón működő monitorizáló szoftver pedig beszerzi, kiértékeli és metrikai információval látja el a szolgáltatást igénybe vevő felhasználót a rendszer webes felületén.
Emellett automatikusan észleli a bejövő metrikus folyamaton belüli problémás állapotokat és értesít az előre meghatározott, kritikus események bekövetkeztekor. 
