#if _WIN32 || _WIN64
#include <stddef.h>
#include <stdint.h>
#include <d3d11.h>
// g++ -O3 -Wall -shared -o DX11Plugin.dll DX11Plugin.cpp -ld3d11
extern "C" void Release(ID3D11Resource** res, ID3D11Device** device, ID3D11DeviceContext** context){
	if(*context != NULL){
		(*context)->Release();
		*context = NULL;
	}
	if(*device != NULL){
		(*device)->Release();
		*device = NULL;
	}
	if(*res != NULL){
		(*res)->Release();
		*res = NULL;
	}
}
extern "C" void ModifyTexturePixels(ID3D11DeviceContext* context, ID3D11Resource* res,
int width, int height, void* dataPtr, uint8_t pixelSize = 4, UINT DstSubresource = 0){
	context->UpdateSubresource(res, DstSubresource, NULL, dataPtr, width * pixelSize, 0);
}
extern "C" void GetInfo(ID3D11Resource* res, ID3D11Device** dev, ID3D11DeviceContext** ctx){
	if(res == NULL) return;
	res->GetDevice(dev);
	(*dev)->GetImmediateContext(ctx);
}
// uint32_t u32 = D3D11CalcSubresource(0, 0, 1);
// D3D11_BOX box;
#endif