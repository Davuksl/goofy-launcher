-- Runs inside the active RCC job. Every request replaces the previous live
-- appearance generation and updates the selected player's current character.
local TargetName = %TARGET%
local Assets = %ASSETS%
local Colors = %COLORS%

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")

local generation = ReplicatedStorage:FindFirstChild("LuckyBloxAppearanceGeneration")
if generation == nil then
	generation = Instance.new("IntValue")
	generation.Name = "LuckyBloxAppearanceGeneration"
	generation.Parent = ReplicatedStorage
end
generation.Value = generation.Value + 1
local myGeneration = generation.Value

local function isTarget(player)
	return TargetName == "" or string.lower(player.Name) == string.lower(TargetName)
end

local function safeIsA(object, className)
	local ok, result = pcall(function() return object:IsA(className) end)
	return ok and result
end

local function applyColors(character)
	if Colors == nil or #Colors < 6 or character == nil then return end
	local bodyColors = character:FindFirstChild("Body Colors")
	if bodyColors == nil then
		bodyColors = Instance.new("BodyColors")
		bodyColors.Name = "Body Colors"
		bodyColors.Parent = character
	end
	local properties = {"HeadColor", "TorsoColor", "LeftArmColor", "RightArmColor", "LeftLegColor", "RightLegColor"}
	for index, property in ipairs(properties) do
		pcall(function() bodyColors[property] = BrickColor.new(Colors[index]) end)
	end
	for _, child in ipairs(character:GetChildren()) do
		if child ~= bodyColors and safeIsA(child, "BodyColors") then
			for index, property in ipairs(properties) do
				pcall(function() child[property] = BrickColor.new(Colors[index]) end)
			end
		end
	end
end

local function clearWearables(character)
	for _, child in ipairs(character:GetChildren()) do
		if safeIsA(child, "Accessory") or safeIsA(child, "Hat") or safeIsA(child, "Shirt") or
			safeIsA(child, "Pants") or safeIsA(child, "CharacterMesh") then
			pcall(function() child:Destroy() end)
		end
	end
	local head = character:FindFirstChild("Head")
	if head ~= nil then
		for _, child in ipairs(head:GetChildren()) do
			if safeIsA(child, "Decal") then pcall(function() child:Destroy() end) end
		end
	end
end

local function attach(object, character)
	if safeIsA(object, "Accessory") or safeIsA(object, "Hat") or safeIsA(object, "Shirt") or
		safeIsA(object, "Pants") or safeIsA(object, "CharacterMesh") then
		object.Parent = character
		return
	end
	if safeIsA(object, "Decal") or safeIsA(object, "Face") then
		local head = character:FindFirstChild("Head")
		if head ~= nil then object.Parent = head end
		return
	end
	if safeIsA(object, "Model") then
		for _, child in ipairs(object:GetChildren()) do
			if safeIsA(child, "Accessory") or safeIsA(child, "Hat") or safeIsA(child, "Shirt") or
				safeIsA(child, "Pants") or safeIsA(child, "CharacterMesh") then
				child.Parent = character
			elseif safeIsA(child, "Decal") or safeIsA(child, "Face") then
				local head = character:FindFirstChild("Head")
				if head ~= nil then child.Parent = head end
			end
		end
	end
end

local function applyAssets(character)
	clearWearables(character)
	for _, url in ipairs(Assets) do
		local ok, objects = pcall(function() return game:GetObjects(url) end)
		if ok and objects ~= nil then
			for _, object in ipairs(objects) do
				pcall(function() attach(object, character) end)
			end
		end
	end
end

local function applyCharacter(character)
	wait(1.2)
	if character == nil or character.Parent == nil or generation.Value ~= myGeneration then return end
	applyAssets(character)
	-- Later server/client generations can finish applying their default
	-- BodyColors after CharacterAdded. Reapply briefly to keep the chosen colors.
	for attempt = 1, 12 do
		if character.Parent == nil or generation.Value ~= myGeneration then return end
		applyColors(character)
		if attempt < 12 then wait(0.5) end
	end
end

local function watch(player)
	if not isTarget(player) then return end
	player.CharacterAdded:connect(applyCharacter)
	if player.Character ~= nil then spawn(function() applyCharacter(player.Character) end) end
end

Players.PlayerAdded:connect(watch)
for _, player in ipairs(Players:GetPlayers()) do watch(player) end
