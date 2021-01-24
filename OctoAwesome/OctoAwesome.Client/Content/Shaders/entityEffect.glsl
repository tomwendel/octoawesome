<Effect>
    <Technique name="Ambient">
        <Pass name="MainPass">
            <Shader type="PixelShader" filename="entityEffect/ambient.ps">

            </Shader>
            <Shader type="VertexShader" filename="entityEffect/entity.vs">

            </Shader>
            <Attributes>
              <attribute name="position">Position</attribute>
              <attribute name="normal">Normal</attribute>
              <attribute name="textureCoordinate">TextureCoordinate</attribute>
            </Attributes>
        </Pass>
    </Technique>
    <Technique name="Shadow">
        <Pass name="MainPass">
            <Shader type="PixelShader" filename="entityEffect/shadow.ps">

            </Shader>
            <Shader type="VertexShader" filename="entityEffect/entity.vs">

            </Shader>
            <Attributes>
              <attribute name="position">Position</attribute>
              <attribute name="normal">Normal</attribute>
              <attribute name="textureCoordinate">TextureCoordinate</attribute>
            </Attributes>
        </Pass>
    </Technique>
</Effect>
