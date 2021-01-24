<Effect>
    <Technique name="Ambient">
        <Pass name="MainPass">
            <Shader type="PixelShader" filename="chunkEffect/ambient.ps">

            </Shader>
            <Shader type="VertexShader" filename="chunkEffect/ambient.vs">

            </Shader>
            <Attributes>
                <attribute name="inputData1">Position</attribute>
              <attribute name="inputData2">Normal</attribute>
            </Attributes>
        </Pass>
    </Technique>
    <Technique name="Shadow">
        <Pass name="MainPass">
            <Shader type="PixelShader" filename="chunkEffect/shadow.ps">

            </Shader>
            <Shader type="VertexShader" filename="chunkEffect/shadow.vs">

            </Shader>
            <Attributes>
                <attribute name="inputData1">Position</attribute>
              <attribute name="inputData2">Normal</attribute>
            </Attributes>
        </Pass>
    </Technique>
</Effect>
