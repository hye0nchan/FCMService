using System.Xml.Serialization;

[XmlRoot("Edge")]
public class EdgeInfo
{
    [XmlElement("Name")]
    public string? Name { get; set; }

    [XmlElement("SerialNumber")]
    public string? SerialNumber { get; set; }

    [XmlElement("FcmToken")]
    public string? FcmToken { get; set; }

    [XmlElement("influxToken")]
    public string? InfluxToken { get; set; }
    
    [XmlElement("influxAddress")]
    public string? InfluxAddress { get; set; }

    [XmlElement("LocalInfluxToken")]
    public string? LocalInfluxToken { get; set; }

    [XmlArray("nodeList")]
    [XmlArrayItem("node")]
    public List<Node>? Nodes { get; set; }
}

public class Node
{
    [XmlElement("nodeSlave")]
    public int NodeSlave { get; set; }

    [XmlArray("SensorList")]
    [XmlArrayItem("Sensor")]
    public List<Sensor>? Sensors { get; set; }
}

public class Sensor
{
    [XmlElement("SensorID")]
    public int SensorID { get; set; }

    [XmlElement("Address")]
    public int SensorAddress { get; set; }
}