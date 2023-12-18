#pragma once
#include "GameConnectionBehaviour.h"

class MultiplayerBehaviour;

class CommonBehaviour : public IGameConnectionBehaviour
{
public:
	virtual void OnAttach(GameConnection& game) override;
	virtual void OnDetach(GameConnection& game) override;
	virtual void OnUpdate(GameConnection& game) override;

private:
	std::weak_ptr<MultiplayerBehaviour> m_MultiplayerBehaviour;
};