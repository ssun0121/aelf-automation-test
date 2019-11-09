using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Utils;
using Google.Protobuf.Reflection;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.ContractSerializer
{
    public class ContractMethod : IComparable
    {
        public MethodDescriptor Descriptor { get; set; }
        public string Name { get; set; }
        public string Input => InputType.Name;
        public MessageDescriptor InputType { get; set; }
        public List<FieldDescriptor> InputFields { get; set; }
        public string Output => OutputType.Name;
        public MessageDescriptor OutputType { get; set; }
        public List<FieldDescriptor> OutputFields { get; set; }

        public ContractMethod(MethodDescriptor method)
        {
            Descriptor = method;
            Name = method.Name;
            InputType = method.InputType;
            OutputType = method.OutputType;
            InputFields = InputType.Fields.InFieldNumberOrder().ToList();
            OutputFields = OutputType.Fields.InFieldNumberOrder().ToList();
        }

        public void GetMethodDescriptionInfo()
        {
            $"[Method]: {Name}".WriteWarningLine();
        }

        public void GetInputParameters()
        {
            $"[Input]: {Input}".WriteWarningLine();
            foreach (var parameter in InputFields)
            {
                if (parameter.Name == "value") continue;
                if (parameter.FieldType == FieldType.Message)
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.MessageType.Name}"
                        .WriteWarningLine();
                else
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.FieldType}"
                        .WriteWarningLine();
            }
        }

        public void GetOutputParameters()
        {
            $"[Output]: {Output}".WriteWarningLine();
            foreach (var parameter in OutputFields)
            {
                if (parameter.Name == "value") continue;
                if (parameter.FieldType == FieldType.Message)
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.MessageType.Name}"
                        .WriteWarningLine();
                else
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.FieldType}"
                        .WriteWarningLine();
            }
        }

        public int CompareTo(object obj)
        {
            ContractMethod info = obj as ContractMethod;
            return string.CompareOrdinal(this.Name, info.Name) > 0 ? 0 : 1;
        }

        public string ParseMethodInputJsonInfo(string[] inputs)
        {
            var output = "";
            var inputJson = new JObject();
            switch (Input)
            {
                case "StringValue":
                    output = $"\"{inputs[0]}\"";
                    break;
                case "Address":
                    inputJson["value"] = inputs[0].ConvertAddress().Value.ToBase64();
                    break;
                case "Hash":
                    inputJson["value"] = HashHelper.HexStringToHash(inputs[0]).Value.ToBase64();
                    break;
                default:
                    for (var i = 0; i < InputFields.Count; i++)
                    {
                        //ignore null parameter
                        if(inputs[i] == "null") continue;
                        var type = InputFields[i];
                        if (type.FieldType == FieldType.Message)
                        {
                            if (type.MessageType.Name == "Address")
                                inputJson[InputFields[i].JsonName] = new JObject
                                {
                                    ["value"] = inputs[i].ConvertAddress().Value.ToBase64()
                                };
                            else if (type.MessageType.Name == "Hash")
                                inputJson[InputFields[i].JsonName] = new JObject
                                {
                                    ["value"] = HashHelper.HexStringToHash(inputs[i]).Value.ToBase64()
                                };
                            else
                            {
                                inputJson[InputFields[i].JsonName] = inputs[i];
                            }
                        }
                        else if (type.FieldType == FieldType.Bool)
                        {
                            inputJson[InputFields[i].JsonName] = bool.Parse(inputs[i]);
                        }
                        else
                        {
                            inputJson[InputFields[i].JsonName] = inputs[i];
                        }
                    }
                    break;
            }

            if (inputJson == new JObject()) return output;
            Log4NetHelper.ConvertJsonString(inputJson.ToString()).WriteWarningLine();
            output = inputJson.ToString();
            return output;
        }
    }
}