using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using WandererWorld.Components;
using WandererWorld.Interfaces;
using WandererWorld.Manager;
using WandererWorld.ShaderContent;
using WandererWorld.Systems;
using WandererWorld.WandererContent;

namespace WandererWorld
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private Matrix wandererWorld;
        private WandererBody wanderer;

        private Shader shader;

        private HeightMapTransformSystem_Wanderer heightMapTransformSystem_Wanderer;

        private Random rnd = new Random();

        private HeightMapComponent heightMapComponent;
        private HeightMapCameraComponent heightMapCameraComponent;

        private HeightmapSystem heightMapSystem;        
        private IUpdateSystem heightMapTranformSystem;
        private IRenderSystem heightMapRenderSystem;

        private CollisionSystem collisionSystem;
        private HouseSystem houseSystem;
        
        private Updater systemsUpdater;
        private Renderer systemRenderer;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            wandererWorld = Matrix.Identity;
            wanderer = new WandererBody(this);
            heightMapTransformSystem_Wanderer = new HeightMapTransformSystem_Wanderer();

            heightMapComponent = new HeightMapComponent();
            heightMapCameraComponent = new HeightMapCameraComponent();

            heightMapSystem = new HeightmapSystem();
            heightMapTranformSystem = new HeightMapTranformSystem();
            heightMapRenderSystem = new HeightMapRenderSystem();      

            collisionSystem = new CollisionSystem();
            houseSystem = new HouseSystem(this);

            systemRenderer = new Renderer();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {           
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Texture2D heightMapTexture2D = Content.Load<Texture2D>("US_Canyon");
            Texture2D heightMapGrassTexture = Content.Load<Texture2D>("grass2");
            Texture2D heightMapFireTexture = Content.Load<Texture2D>("fire");
            Texture2D BrickTexture = Content.Load<Texture2D>("wall_texture");
            Model robotModel = Content.Load<Model>("Lab2Model");
            Texture2D robotTexture = Content.Load<Texture2D>("robot_texture");
            Texture2D roofTexture = Content.Load<Texture2D>("roof");
            Texture2D timberWallTexture = Content.Load<Texture2D>("timber_wall");
            Texture2D timberRoofTexture = Content.Load<Texture2D>("timber_roof");

            //This is new
            Effect shaderEffect = Content.Load<Effect>("TestShaders");
            shader = new Shader(shaderEffect);

            heightMapComponent = new HeightMapComponent
            {
                HeightMap = heightMapTexture2D,
                HeightMapTexture = heightMapGrassTexture,
                GraphicsDevice = graphics.GraphicsDevice,
                Width = heightMapTexture2D.Width,
                Height = heightMapTexture2D.Height,
                HeightMapData = new float[heightMapTexture2D.Width, heightMapTexture2D.Height]
            };

            heightMapCameraComponent = new HeightMapCameraComponent()
            {
                ViewMatrix = Matrix.CreateLookAt(new Vector3(-100, 0, 0), Vector3.Zero, Vector3.Up),
                ProjectionMatrix = Matrix.CreatePerspective(1.2f, 0.9f, 1.0f, 1000.0f),
                TerrainMatrix = Matrix.CreateTranslation(new Vector3(0, -100, 256)),
                Position = new Vector3(-100, 0, 0),
                Direction = Vector3.Zero,
                Movement = new Vector3(1, 1, 1),
                Rotation = new Vector3(2, 2, 2) * 0.01f,
                TerrainPosition = new Vector3(0, -100, 256),
            };
            heightMapCameraComponent.Frustum = new BoundingFrustum(heightMapCameraComponent.ViewMatrix * heightMapCameraComponent.ProjectionMatrix);

            int heightMapId = EntityComponentManager.GetManager().CreateNewEntityId();
            EntityComponentManager.GetManager().AddComponentToEntity(heightMapId, heightMapComponent);
            EntityComponentManager.GetManager().AddComponentToEntity(heightMapId, heightMapCameraComponent);

            CreateRandomHouses(10, BrickTexture, roofTexture, timberWallTexture, timberRoofTexture);

            heightMapSystem.CreateHeightMaps();            
        }

        private void CreateRandomHouses(int nHouses, Texture2D wall1, Texture2D roof1, Texture2D wall2, Texture2D roof2)
        {
            // Max and min values
            short maxScale = 110;
            short minScale = 70;

            short minX = 100;
            short maxX = 950;
            short minZ = -900;
            short maxZ = -100;

            for(int i = 0; i < nHouses; ++i)
            {
                var s = rnd.Next(minScale, maxScale);
                Vector3 scale = new Vector3(s += rnd.Next(0, 20), s + rnd.Next(0, 20), s + rnd.Next(0, 20));
                Vector3 pos = new Vector3(rnd.Next(minX, maxX), scale.Y / 2 + 20, rnd.Next(minZ, maxZ));
                var houses = (EntityComponentManager.GetManager().GetComponentByType(typeof(HouseComponent)).Values);
                bool ok = true;
                foreach (var h in houses)
                {
                    var v = (HouseComponent)h;
                    if(Vector3.Distance(v.Position, pos) < 150){
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    var wall = wall1;
                    var roof = roof1;
                    if (rnd.NextDouble() < .5)
                    {
                        wall = wall2;
                        roof = roof2;

                        shader.RealisticSettings();
                    }


                    HouseComponent house = new HouseComponent(scale, pos, Matrix.CreateRotationY((float)(rnd.NextDouble() * (Math.PI * 2))), wall, roof);

                    int hid = EntityComponentManager.GetManager().CreateNewEntityId();
                    EntityComponentManager.GetManager().AddComponentToEntity(hid, house);

                    HouseBoundingBox hBox = new HouseBoundingBox()
                    {
                        BoundingBox = new BoundingBox(pos, pos + scale)
                    };
                    EntityComponentManager.GetManager().AddComponentToEntity(hid, hBox);
                }
                else
                {
                    --i;
                }
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            systemsUpdater = new Updater(gameTime);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            wanderer.UpdateLimbMovement(gameTime);
            heightMapTransformSystem_Wanderer.UpdateHeightMap_Wanderer(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {            
            GraphicsDevice.Clear(Color.CornflowerBlue);            

            systemRenderer.Render(heightMapRenderSystem);
            houseSystem.Update();
            wanderer.DrawLimb(gameTime, wandererWorld);

            base.Draw(gameTime);
        }
    }
}
