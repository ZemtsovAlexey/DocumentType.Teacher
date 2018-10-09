import React, {Component} from 'react';
import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr';
import { Col, Row, Thumbnail, ButtonGroup, Button, FormGroup, FormControl } from 'react-bootstrap';
import { NetSettings } from './NetSettings';

export class DocumentAngelTeacher extends Component {
    displayName = DocumentAngelTeacher.name;

    constructor(props) {
        super(props);
        this.state = {
            hubConnection: null,
            file: null,
            fileUploading: false,
            iteration: 0,
            error: 0,
            successes: 0,
            successPercent: 0,
            imageSrc: '/thumbnail.png',
            learningRate: 0.03,
            batchImagePath: '',
            batchImageIndex: 0,
            batchTarget: 0,
            loadNetRun: false,
            netFile: null,
            preparedBatch: false
        };

        this.onFormSubmit = this.onFormSubmit.bind(this);
        this.onChange = this.onChange.bind(this);
        this.fileUpload = this.fileUpload.bind(this);
    }

    componentDidMount = async () => {
        const hubConnection = new HubConnectionBuilder()
            .withUrl(`${document.location.protocol}//${document.location.host}/teacherHub`)
            .configureLogging(LogLevel.Information)
            .build();

        this.setState({hubConnection}, () => {
            this.state.hubConnection
               .start()
               .then(() => console.log('Connection started!'))
               .catch(err => console.log('Error while establishing connection :('));

            this.state.hubConnection.on('AngelNetChange', (result) => {
                this.setState({ 
                    iteration: result.iteration, 
                    error: result.error, 
                    successes: result.successes, 
                    successPercent: result.successPercent,
                    batchImageIndex: result.imageIndex,
                    batchTarget: result.target});
                
                // this.showBatchImage();
            });
        });

        // await this.getLearningRate();
    };

    onFormSubmit = async (e) => {
        e.preventDefault();
        this.setState({ fileUploading: true });

        this.fileUpload(this.state.file)
            .then(response => response.blob())
            .then(images => {
                let outside = URL.createObjectURL(images);
                this.setState({ imageSrc: outside, fileUploading: false });
            });
    };
    
    onChange(e) {
        this.setState({file:e.target.files[0]})
    }

    fileUpload(file){
        let data = new FormData();
        data.append('file', file);
        
        return fetch('api/net/angel/Compute', {
            method: 'POST',
            body: data
        });
    }

    async teachRun(){
        await fetch('api/net/angel/teach/run', {
            method: 'POST'
        });
    }

    async teachStop(){
        await fetch('api/net/angel/teach/stop', {
            method: 'POST'
        });
    }

    getLearningRate = async () => {
        await fetch('api/net/angel/teach/learningRate')
            .then((data) => data.data)
            .then((data) => { this.setState({learningRate: data}) })
    };

    setLearningRate = async (event) => {
        let value = event.target.value;
        
        await fetch(`api/net/angel/teach/learningRate/${value}`, { method: 'POST' })
            .then(() => { this.setState({learningRate: value}) })
    };
    
    changeBatchImageIndex = (e) => {
        this.setState({batchImageIndex: e.target.value});
    };

    changeBatchTarget = (e) => {
        this.setState({batchTarget: e.target.value});
    };
    
    showBatchImage = () => {
        this.setState({ batchImagePath: `/api/net/angel/teach/batch/image/${this.state.batchImageIndex}/${this.state.batchTarget}`});
    };

    saveNet = () => {
        window.open('api/net/angel/save');
    };

    loadNet = async () => {
        this.setState({loadNetRun: true});

        let data = new FormData();
        data.append('file', this.state.netFile);

        await fetch('api/net/angel/load', {
            method: 'POST',
            body: data
        }).then(async () => {
            this.setState({loadNetRun: false});
        });
    };

    prepareTeachBatchFile = async () => {
        this.setState({preparedBatch: true});
        
        await fetch('api/net/angel/teach/batch', {
            method: 'POST'
        }).then(() => { this.setState({preparedBatch: false}); });
    };
    
    render() {
        return (
            <div style={{ margin: '0 0 25px 0' }}>
                <h1>Teacher</h1>

                <Row>
                    <Col sm={2}>
                        <ButtonGroup>
                            <Button onClick={this.teachRun}>teach run</Button>
                            <Button onClick={this.teachStop}>teach stop</Button>
                        </ButtonGroup>
                    </Col>
                    <Col sm={1}>
                    </Col>
                    <Col sm={1}>
                        <label>Iteration: </label>
                        <label>{this.state.iteration}</label>
                    </Col>
                    <Col sm={2}>
                        <label>Error: </label>
                        <label>{this.state.error}</label>
                    </Col>
                    <Col sm={3}>
                        <label>Successes: </label>
                        <label>{this.state.successes}</label>
                        <label>&nbsp;&nbsp;{this.state.successPercent}%</label>
                    </Col>
                </Row>
                
                <Row>
                    <Col sm={4}>
                        <label>Learning rate:&nbsp;</label>
                        <input type='text' onChange={this.setLearningRate} value={this.state.learningRate}/>
                    </Col>
                </Row>

                <Row>
                    <Col sm={12}>
                        <ButtonGroup>
                            {/*<Button bsStyle="primary" onClick={this.applySettings}>Apply</Button>*/}
                            <Button bsStyle="primary" onClick={this.prepareTeachBatchFile} disabled={this.state.preparedBatch}>Prepare batch</Button>
                            <Button bsStyle="primary" onClick={this.saveNet}>Save</Button>
                        </ButtonGroup>
                        <form onSubmit={this.onFormSubmit} style={{display: "inline"}}>
                            <input type="file" onChange={(e) => { this.setState({netFile: e.target.files[0]}); }} style={{display: 'inline-block', border: '1px solid silver'}} />
                            <Button bsStyle="primary" onClick={this.loadNet} disabled={this.state.loadNetRun}>Load Net</Button>
                        </form>
                    </Col>
                </Row>

                <Row>
                    <Col sm={12}>
                        <form onSubmit={this.onFormSubmit} style={{display: "inline"}}>
                            <h1>File Upload</h1>
                            <input type="file" onChange={this.onChange} style={{display: 'inline-block', border: '1px solid silver'}} />
                            <Button type="submit" bsStyle="primary" disabled={this.state.fileUploading}>Upload</Button>
                        </form>
                    </Col>
                </Row>
                
                <Row>
                    <Col sm={7}>
                        <Thumbnail href="#" alt="171x180" src={this.state.imageSrc} />
                    </Col>
                </Row>
                
                <Row>
                    <Col sm={12}>
                        <input type='number' onChange={this.changeBatchImageIndex} value={this.state.batchImageIndex}/>
                        <input type='number' onChange={this.changeBatchTarget} value={this.state.batchTarget}/>
                        <button onClick={this.showBatchImage}>show</button>
                    </Col>
                </Row>

                <Row>
                    <Col sm={12}>
                        <img src={this.state.batchImagePath}/>
                    </Col>
                </Row>
            </div>
        );
    }
}
