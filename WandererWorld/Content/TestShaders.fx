#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix World;
matrix View;
matrix Projection;
matrix WorldInverseTranspose;

float4 AmbientColor;
float AmbientIntensity;

float3 DiffuseLightDirection;
float4 DiffuseColor;
float DiffuseIntensity;

float Shininess;
float4 SpecularColor;
float SpecularIntensity;

float3 ViewVector = float3(1, 0, 0);

///FOG
float FogStart = 40;
float FogEnd = 100;
float4 FogColor = float4(1, 0, 0, 1);
bool FogEnabled = 0;
float3 CameraPosition;

bool TextureEnabled = 1;
texture ModelTexture;

sampler2D textureSampler = sampler_state
{
    Texture = (ModelTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};


struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float3 Normal : TEXCOORD0;
    float2 TextureCoordinate : TEXCOORD1;
    float fogFactor : FOG;
};


//SamplerState SampleType
//{
//    //Filter = MIN_MAG_MIP_LINEAR;
//    AddressU = Wrap;
//    AddressV = Wrap;
//};

float ComputeFogFactor(float d)
{
    //d is the distance to the geometry sampling from the camera
    //this simply returns a value that interpolates from 0 to 1 
    //with 0 starting at FogStart and 1 at FogEnd 
    return clamp((d - FogStart) / (FogEnd - FogStart), 0, 1) * FogEnabled;
}

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
    float distance;

	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	float4 normal = mul(input.Normal, WorldInverseTranspose);
	float lightIntensity = dot(normal.xyz, DiffuseLightDirection);

    output.Position = mul(viewPosition, Projection);
    output.TextureCoordinate = input.TextureCoordinate;
	output.Color = saturate(DiffuseColor * DiffuseIntensity * lightIntensity);
	output.Normal = normal.xyz;

    distance = length(worldPosition.xyz - CameraPosition);
    output.fogFactor = saturate(ComputeFogFactor(distance));

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 normal = normalize(input.Normal);
    float4 returnColor = { 1, 1, 1, 1 };
    float4 textureColor = { 0, 0, 0, 0 };
    float4 lights = { 0, 0, 0, 0 };


    lights += AmbientColor * AmbientIntensity;

    if (TextureEnabled)
    {
        textureColor = tex2D(textureSampler, input.TextureCoordinate);
        lights = float4(0, 0, 0, 0);
        lights += AmbientColor * AmbientIntensity * textureColor;
    }

    float nl = max(0, dot(normalize(DiffuseLightDirection), normal));
    lights += DiffuseIntensity * DiffuseColor * nl;
    float3 light = normalize(DiffuseLightDirection);
    float3 r = normalize(2 * dot(light, normal) * normal - light);
    float3 v = normalize(ViewVector);

    float dotProduct = dot(r, v);
    float4 specular = SpecularIntensity * SpecularColor * max(pow(abs(dotProduct), Shininess), 0);
				
    lights += specular;
		
    returnColor = saturate(returnColor * lights);
	
    returnColor.a = 1;
	
    if (FogEnabled)
    {
        return lerp(returnColor, FogColor, input.fogFactor);
    }
    return returnColor;

}




technique Ambient
{
    pass Pass1
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}



